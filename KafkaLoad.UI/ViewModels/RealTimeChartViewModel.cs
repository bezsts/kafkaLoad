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
    private double _lastProdLatSum = 0;

    private long _lastConsBytes = 0;
    private long _lastConsMsgs = 0;
    private long _lastConsErrors = 0;
    private double _lastConsLatSum = 0;

    // --- SLIDING WINDOW SMOOTHING (8 samples × 500ms = 4 seconds) ---
    private const int SmoothingSamples = 8;
    private readonly SlidingWindowRate _swProdMsgRate   = new(SmoothingSamples);
    private readonly SlidingWindowRate _swProdMbRate    = new(SmoothingSamples);
    private readonly SlidingWindowRate _swProdErrRate   = new(SmoothingSamples);
    private readonly SlidingWindowRate _swProdLatency   = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsMsgRate   = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsMbRate    = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsErrRate   = new(SmoothingSamples);
    private readonly SlidingWindowRate _swConsLatency   = new(SmoothingSamples);

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
        double prodErrSec = (snapshot.Producer.ErrorMessages - _lastProdErrors) / deltaSec;

        double consMbSec = ((snapshot.Consumer.TotalBytesConsumed - _lastConsBytes) / 1024.0 / 1024.0) / deltaSec;
        double consMsgSec = (snapshot.Consumer.SuccessMessagesConsumed - _lastConsMsgs) / deltaSec;
        double consErrSec = (snapshot.Consumer.ErrorMessagesConsumed - _lastConsErrors) / deltaSec;

        // --- CALCULATE INSTANTANEOUS LATENCY ---
        double currentProdLatSum = snapshot.Producer.LatencySumMs;
        double currentConsLatSum = snapshot.Consumer.LatencySumMs;

        double deltaProdMsgs = snapshot.Producer.SuccessMessagesSent - _lastProdMsgs;
        double deltaConsMsgs = snapshot.Consumer.SuccessMessagesConsumed - _lastConsMsgs;

        double instProdLat = deltaProdMsgs > 0 ? Math.Max(0, (currentProdLatSum - _lastProdLatSum) / deltaProdMsgs) : 0;
        double instConsLat = deltaConsMsgs > 0 ? Math.Max(0, (currentConsLatSum - _lastConsLatSum) / deltaConsMsgs) : 0;

        // Prevent negative values in case of counter resets
        prodMbSec = Math.Max(0, prodMbSec);
        prodMsgSec = Math.Max(0, prodMsgSec);
        prodErrSec = Math.Max(0, prodErrSec);
        consMbSec = Math.Max(0, consMbSec);
        consMsgSec = Math.Max(0, consMsgSec);
        consErrSec = Math.Max(0, consErrSec);

        // --- SMOOTH RATES OVER A 4-SECOND SLIDING WINDOW ---

        double smoothProdMsgSec  = _swProdMsgRate.Add(prodMsgSec  * deltaSec, deltaSec);
        double smoothProdMbSec   = _swProdMbRate .Add(prodMbSec   * deltaSec, deltaSec);
        double smoothProdErrSec  = _swProdErrRate.Add(prodErrSec  * deltaSec, deltaSec);
        double smoothConsMsgSec  = _swConsMsgRate.Add(consMsgSec  * deltaSec, deltaSec);
        double smoothConsMbSec   = _swConsMbRate .Add(consMbSec   * deltaSec, deltaSec);
        double smoothConsErrSec  = _swConsErrRate.Add(consErrSec  * deltaSec, deltaSec);

        double smoothProdLatency = deltaProdMsgs > 0
            ? _swProdLatency.Add(instProdLat * deltaProdMsgs, deltaProdMsgs)
            : _swProdLatency.Add(0, 0);
        double smoothConsLatency = deltaConsMsgs > 0
            ? _swConsLatency.Add(instConsLat * deltaConsMsgs, deltaConsMsgs)
            : _swConsLatency.Add(0, 0);

        // --- UPDATE BUFFERS ---

        // Producer
        ProducerThroughput.AddPoint(time, smoothProdMbSec);
        ProducerMsgRate.AddPoint(time, smoothProdMsgSec);
        ProducerErrors.AddPoint(time, smoothProdErrSec);
        ProducerLatency.AddPoint(time, smoothProdLatency);

        // Consumer
        ConsumerThroughput.AddPoint(time, smoothConsMbSec);
        ConsumerMsgRate.AddPoint(time, smoothConsMsgSec);
        ConsumerErrors.AddPoint(time, smoothConsErrSec);
        ConsumerLatency.AddPoint(time, smoothConsLatency);

        // --- SAVE STATE FOR NEXT CALCULATION ---
        UpdateLastValues(snapshot);
        _lastTimeSec = time;

        RefreshCounter++;
    }

    private void UpdateLastValues(GlobalMetricsSnapshot snapshot)
    {
        _lastProdBytes = snapshot.Producer.TotalBytesSent;
        _lastProdMsgs = snapshot.Producer.SuccessMessagesSent;
        _lastProdErrors = snapshot.Producer.ErrorMessages;
        _lastProdLatSum = snapshot.Producer.LatencySumMs;

        _lastConsBytes = snapshot.Consumer.TotalBytesConsumed;
        _lastConsMsgs = snapshot.Consumer.SuccessMessagesConsumed;
        _lastConsErrors = snapshot.Consumer.ErrorMessagesConsumed;
        _lastConsLatSum = snapshot.Consumer.LatencySumMs;
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
        _lastProdLatSum = 0;
        _lastConsBytes = 0;
        _lastConsMsgs = 0;
        _lastConsErrors = 0;
        _lastConsLatSum = 0;

        _swProdMsgRate.Reset();
        _swProdMbRate.Reset();
        _swProdErrRate.Reset();
        _swProdLatency.Reset();
        _swConsMsgRate.Reset();
        _swConsMbRate.Reset();
        _swConsErrRate.Reset();
        _swConsLatency.Reset();

        RefreshCounter = 0;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}