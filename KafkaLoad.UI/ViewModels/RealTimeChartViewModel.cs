using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Metrics;
using KafkaLoad.Core.Models.Reports;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.UI.Helpers;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;

namespace KafkaLoad.UI.ViewModels;

public class RealTimeChartViewModel : ReactiveObject, IDisposable
{
    private readonly IMetricsService _metricsService;
    private IDisposable? _subscription;

    // Buffers for charts
    public MetricSeriesBuffer ProducerThroughput { get; }
    public MetricSeriesBuffer ProducerLatencyP50 { get; }
    public MetricSeriesBuffer ProducerLatencyP95 { get; }
    public MetricSeriesBuffer ProducerLatencyP99 { get; }
    public MetricSeriesBuffer ProducerMsgRate { get; }
    public MetricSeriesBuffer ProducerErrors { get; }

    public MetricSeriesBuffer ConsumerThroughput { get; }
    public MetricSeriesBuffer ConsumerLatencyP50 { get; }
    public MetricSeriesBuffer ConsumerLatencyP95 { get; }
    public MetricSeriesBuffer ConsumerLatencyP99 { get; }
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

    // --- SLIDING WINDOW SMOOTHING (8 samples × 500ms = 4 seconds) ---
    private const int SmoothingSamples = 8;
    private readonly SlidingWindowRate _swProdMsgRate = new(SmoothingSamples);
    private readonly SlidingWindowRate _swProdMbRate  = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsMsgRate = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsMbRate  = new(SmoothingSamples);

    public RealTimeChartViewModel(IMetricsService metricsService)
    {
        _metricsService = metricsService;

        ProducerThroughput = new MetricSeriesBuffer("Producer MB/s");
        ProducerLatencyP50 = new MetricSeriesBuffer("Producer P50 Latency (ms)");
        ProducerLatencyP95 = new MetricSeriesBuffer("Producer P95 Latency (ms)");
        ProducerLatencyP99 = new MetricSeriesBuffer("Producer P99 Latency (ms)");
        ProducerMsgRate = new MetricSeriesBuffer("Producer Msg/s");
        ProducerErrors = new MetricSeriesBuffer("Producer Errors/sec");

        ConsumerThroughput = new MetricSeriesBuffer("Consumer MB/s");
        ConsumerLatencyP50 = new MetricSeriesBuffer("Consumer P50 Latency (ms)");
        ConsumerLatencyP95 = new MetricSeriesBuffer("Consumer P95 Latency (ms)");
        ConsumerLatencyP99 = new MetricSeriesBuffer("Consumer P99 Latency (ms)");
        ConsumerMsgRate = new MetricSeriesBuffer("Consumer Msg/s");
        ConsumerErrors = new MetricSeriesBuffer("Consumer Errors/sec");

        // Sampling at 500ms ensures smooth UI updates without freezing
        _subscription = _metricsService.MetricsStream
            .Sample(TimeSpan.FromMilliseconds(500))
            .ObserveOn(AvaloniaScheduler.Instance)
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
        double prodMsgSec = (snapshot.Producer.SuccessMessagesSent - _lastProdMsgs) / deltaSec;
        long deltaProdErrors = snapshot.Producer.TotalErrors - _lastProdErrors;

        double consMbSec = ((snapshot.Consumer.TotalBytesConsumed - _lastConsBytes) / 1024.0 / 1024.0) / deltaSec;
        double consMsgSec = (snapshot.Consumer.SuccessMessagesConsumed - _lastConsMsgs) / deltaSec;
        long deltaConsErrors = snapshot.Consumer.ErrorMessagesConsumed - _lastConsErrors;

        // Prevent negative values in case of counter resets
        prodMbSec = Math.Max(0, prodMbSec);
        prodMsgSec = Math.Max(0, prodMsgSec);
        deltaProdErrors = Math.Max(0, deltaProdErrors);
        consMbSec = Math.Max(0, consMbSec);
        consMsgSec = Math.Max(0, consMsgSec);
        deltaConsErrors = Math.Max(0, deltaConsErrors);

        // --- SMOOTH RATES OVER A 4-SECOND SLIDING WINDOW ---

        double smoothProdMsgSec = _swProdMsgRate.Add(prodMsgSec * deltaSec, deltaSec);
        double smoothProdMbSec  = _swProdMbRate .Add(prodMbSec  * deltaSec, deltaSec);
        double smoothConsMsgSec = _swConsMsgRate.Add(consMsgSec * deltaSec, deltaSec);
        double smoothConsMbSec  = _swConsMbRate .Add(consMbSec  * deltaSec, deltaSec);

        // --- UPDATE BUFFERS ---

        // Producer
        ProducerThroughput.AddPoint(time, smoothProdMbSec);
        ProducerMsgRate.AddPoint(time, smoothProdMsgSec);
        ProducerErrors.AddPoint(time, deltaProdErrors);

        // Producer latency: use running cumulative percentiles from histogram
        ProducerLatencyP50.AddPoint(time, snapshot.Producer.P50Lat);
        ProducerLatencyP95.AddPoint(time, snapshot.Producer.P95Lat);
        ProducerLatencyP99.AddPoint(time, snapshot.Producer.P99Lat);

        // Consumer
        ConsumerThroughput.AddPoint(time, smoothConsMbSec);
        ConsumerMsgRate.AddPoint(time, smoothConsMsgSec);
        ConsumerErrors.AddPoint(time, deltaConsErrors);

        // Consumer latency: use running cumulative percentiles from histogram
        ConsumerLatencyP50.AddPoint(time, snapshot.Consumer.P50Lat);
        ConsumerLatencyP95.AddPoint(time, snapshot.Consumer.P95Lat);
        ConsumerLatencyP99.AddPoint(time, snapshot.Consumer.P99Lat);

        // --- SAVE STATE FOR NEXT CALCULATION ---
        UpdateLastValues(snapshot);
        _lastTimeSec = time;

        RefreshCounter++;
    }

    private void UpdateLastValues(GlobalMetricsSnapshot snapshot)
    {
        _lastProdBytes = snapshot.Producer.TotalBytesSent;
        _lastProdMsgs = snapshot.Producer.SuccessMessagesSent;
        _lastProdErrors = snapshot.Producer.TotalErrors;

        _lastConsBytes = snapshot.Consumer.TotalBytesConsumed;
        _lastConsMsgs = snapshot.Consumer.SuccessMessagesConsumed;
        _lastConsErrors = snapshot.Consumer.ErrorMessagesConsumed;
    }

    public Dictionary<string, List<TimeSeriesPoint>> GetTimeSeriesData()
    {
        var result = new Dictionary<string, List<TimeSeriesPoint>>();

        void AddSeries(string key, MetricSeriesBuffer buffer)
        {
            var points = new List<TimeSeriesPoint>();
            try
            {
                var xs = buffer.XValues.ToArray();
                var ys = buffer.YValues.ToArray();

                int count = Math.Min(xs.Length, ys.Length);

                for (int i = 0; i < count; i++)
                {
                    points.Add(new TimeSeriesPoint(xs[i], ys[i]));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting real-time chart data for series: {SeriesKey}", key);
            }
            result.Add(key, points);
        }

        AddSeries("Producer_MsgRate", ProducerMsgRate);
        AddSeries("Producer_ThroughputMB", ProducerThroughput);
        AddSeries("Producer_Latency_P50", ProducerLatencyP50);
        AddSeries("Producer_Latency_P95", ProducerLatencyP95);
        AddSeries("Producer_Latency_P99", ProducerLatencyP99);
        AddSeries("Producer_Errors", ProducerErrors);

        AddSeries("Consumer_MsgRate", ConsumerMsgRate);
        AddSeries("Consumer_ThroughputMB", ConsumerThroughput);
        AddSeries("Consumer_Latency_P50", ConsumerLatencyP50);
        AddSeries("Consumer_Latency_P95", ConsumerLatencyP95);
        AddSeries("Consumer_Latency_P99", ConsumerLatencyP99);
        AddSeries("Consumer_Errors", ConsumerErrors);

        return result;
    }

    public void Reset()
    {
        ProducerThroughput.Clear();
        ProducerLatencyP50.Clear();
        ProducerLatencyP95.Clear();
        ProducerLatencyP99.Clear();
        ProducerMsgRate.Clear();
        ProducerErrors.Clear();

        ConsumerThroughput.Clear();
        ConsumerLatencyP50.Clear();
        ConsumerLatencyP95.Clear();
        ConsumerLatencyP99.Clear();
        ConsumerMsgRate.Clear();
        ConsumerErrors.Clear();

        _lastTimeSec = 0;
        _lastProdBytes = 0;
        _lastProdMsgs = 0;
        _lastProdErrors = 0;
        _lastConsBytes = 0;
        _lastConsMsgs = 0;
        _lastConsErrors = 0;

        _swProdMsgRate.Reset();
        _swProdMbRate.Reset();
        _swConsMsgRate.Reset();
        _swConsMbRate.Reset();

        RefreshCounter = 0;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
