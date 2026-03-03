using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Models.Reports;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.Services.Visualization;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace KafkaLoad.Desktop.ViewModels;

public class RealTimeChartViewModel : ReactiveObject, IDisposable
{
    private readonly IMetricsService _metricsService;
    private IDisposable? _subscription;

    // Buffers for charts
    public MetricSeriesBuffer ProducerThroughput { get; }
    public MetricSeriesBuffer ProducerLatency { get; }
    public MetricSeriesBuffer ProducerMsgRate { get; }
    public MetricSeriesBuffer ProducerErrors { get; }

    public MetricSeriesBuffer ConsumerThroughput { get; }
    public MetricSeriesBuffer ConsumerLatency { get; }
    public MetricSeriesBuffer ConsumerMsgRate { get; }
    public MetricSeriesBuffer ConsumerErrors { get; }

    private long _refreshCounter;
    public long RefreshCounter
    {
        get => _refreshCounter;
        set => this.RaiseAndSetIfChanged(ref _refreshCounter, value);
    }

    // --- STATE FOR INSTANTANEOUS RATE CALCULATION ---
    private double _lastTimeSec = 0;

    private long _lastProdBytes = 0;
    private long _lastProdMsgs = 0;
    private long _lastProdErrors = 0;

    private long _lastConsBytes = 0;
    private long _lastConsMsgs = 0;
    private long _lastConsErrors = 0;

    public RealTimeChartViewModel(IMetricsService metricsService)
    {
        _metricsService = metricsService;

        ProducerThroughput = new MetricSeriesBuffer("Producer MB/s");
        ProducerLatency = new MetricSeriesBuffer("Producer Latency (ms)");
        ProducerMsgRate = new MetricSeriesBuffer("Producer Msg/s");
        ProducerErrors = new MetricSeriesBuffer("Producer Errors/sec");

        ConsumerThroughput = new MetricSeriesBuffer("Consumer MB/s");
        ConsumerLatency = new MetricSeriesBuffer("Consumer Latency (ms)");
        ConsumerMsgRate = new MetricSeriesBuffer("Consumer Msg/s");
        ConsumerErrors = new MetricSeriesBuffer("Consumer Errors/sec");

        // Sampling at 500ms ensures smooth UI updates without freezing
        _subscription = _metricsService.MetricsStream
            .Sample(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnMetricsReceived);
    }

    private void OnMetricsReceived(GlobalMetricsSnapshot snapshot)
    {
        double time = snapshot.Duration.TotalSeconds;

        // Ignore invalid time during initialization
        if (time > 300_000_000) return;

        if (time < _lastTimeSec)
        {
            Reset();
        }

        double deltaSec = time - _lastTimeSec;

        // Skip the very first snapshot or if time hasn't moved forward
        if (deltaSec <= 0.001)
        {
            _lastTimeSec = time;
            UpdateLastValues(snapshot);
            return;
        }

        // --- CALCULATE INSTANTANEOUS RATES (DELTAS) ---

        double prodMbSec = ((snapshot.Producer.TotalBytesSent - _lastProdBytes) / 1024.0 / 1024.0) / deltaSec;
        double prodMsgSec = (snapshot.Producer.TotalMsgsSent - _lastProdMsgs) / deltaSec;
        double prodErrSec = (snapshot.Producer.ErrorMsgsSent - _lastProdErrors) / deltaSec;

        double consMbSec = ((snapshot.Consumer.TotalBytesConsumed - _lastConsBytes) / 1024.0 / 1024.0) / deltaSec;
        double consMsgSec = (snapshot.Consumer.TotalMsgConsumed - _lastConsMsgs) / deltaSec;
        double consErrSec = (snapshot.Consumer.ErrorMsgsConsumed - _lastConsErrors) / deltaSec;

        // Prevent negative values in case of counter resets
        prodMbSec = Math.Max(0, prodMbSec);
        prodMsgSec = Math.Max(0, prodMsgSec);
        prodErrSec = Math.Max(0, prodErrSec);
        consMbSec = Math.Max(0, consMbSec);
        consMsgSec = Math.Max(0, consMsgSec);
        consErrSec = Math.Max(0, consErrSec);

        // --- UPDATE BUFFERS ---

        // Producer
        ProducerThroughput.AddPoint(time, prodMbSec);
        ProducerMsgRate.AddPoint(time, prodMsgSec);
        ProducerErrors.AddPoint(time, prodErrSec);
        ProducerLatency.AddPoint(time, snapshot.Producer.AvgLatMs); // Latency is already an average, so we keep it

        // Consumer
        ConsumerThroughput.AddPoint(time, consMbSec);
        ConsumerMsgRate.AddPoint(time, consMsgSec);
        ConsumerErrors.AddPoint(time, consErrSec);
        ConsumerLatency.AddPoint(time, snapshot.Consumer.AvgLatencyMs);

        // --- SAVE STATE FOR NEXT CALCULATION ---
        UpdateLastValues(snapshot);
        _lastTimeSec = time;

        RefreshCounter++;
    }

    private void UpdateLastValues(GlobalMetricsSnapshot snapshot)
    {
        _lastProdBytes = snapshot.Producer.TotalBytesSent;
        _lastProdMsgs = snapshot.Producer.TotalMsgsSent;
        _lastProdErrors = snapshot.Producer.ErrorMsgsSent;

        _lastConsBytes = snapshot.Consumer.TotalBytesConsumed;
        _lastConsMsgs = snapshot.Consumer.TotalMsgConsumed;
        _lastConsErrors = snapshot.Consumer.ErrorMsgsConsumed;
    }

    public Dictionary<string, List<TimeSeriesPoint>> GetTimeSeriesData()
    {
        var result = new Dictionary<string, List<TimeSeriesPoint>>();

        void AddSeries(string key, MetricSeriesBuffer buffer)
        {
            var points = new List<TimeSeriesPoint>();
            var xs = buffer.XValues.ToArray();
            var ys = buffer.YValues.ToArray();

            int count = Math.Min(xs.Length, ys.Length);

            for (int i = 0; i < count; i++)
            {
                points.Add(new TimeSeriesPoint(xs[i], ys[i]));
            }
            result.Add(key, points);
        }

        AddSeries("Producer_MsgRate", ProducerMsgRate);
        AddSeries("Producer_ThroughputMB", ProducerThroughput);
        AddSeries("Producer_Latency", ProducerLatency);
        AddSeries("Producer_Errors", ProducerErrors);

        AddSeries("Consumer_MsgRate", ConsumerMsgRate);
        AddSeries("Consumer_ThroughputMB", ConsumerThroughput);
        AddSeries("Consumer_Latency", ConsumerLatency);
        AddSeries("Consumer_Errors", ConsumerErrors);

        return result;
    }

    public void Reset()
    {
        ProducerThroughput.Clear();
        ProducerLatency.Clear();
        ProducerMsgRate.Clear();
        ProducerErrors.Clear();

        ConsumerThroughput.Clear();
        ConsumerLatency.Clear();
        ConsumerMsgRate.Clear();
        ConsumerErrors.Clear();

        _lastTimeSec = 0;
        _lastProdBytes = 0;
        _lastProdMsgs = 0;
        _lastProdErrors = 0;
        _lastConsBytes = 0;
        _lastConsMsgs = 0;
        _lastConsErrors = 0;

        RefreshCounter = 0;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}