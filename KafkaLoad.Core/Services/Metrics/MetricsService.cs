using KafkaLoad.Core.Models.Metrics;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Reactive.Linq;

namespace KafkaLoad.Core.Services;

public class MetricsService : IMetricsService
{
    private readonly ProducerMetricsAccumulator _producerAccumulator = new();
    private readonly ConsumerMetricsAccumulator _consumerAccumulator = new();
    private DateTime _startTime;
    private DateTime? _endTime;
    public IObservable<GlobalMetricsSnapshot> MetricsStream =>
        Observable.Interval(TimeSpan.FromSeconds(1))
                  .Select(_ => CreateSnapshot());

    public GlobalMetricsSnapshot CurrentSnapshot => CreateSnapshot();

    public void RecordProducerSuccess(int bytes, double latencyMs) =>
        _producerAccumulator.AddSuccess(bytes, latencyMs);
    public void RecordConsumerSuccess(int bytes, double latencyMs) =>
        _consumerAccumulator.AddSuccess(bytes, latencyMs);

    public void RecordProducerError() => _producerAccumulator.AddError();
    public void RecordConsumerError() => _consumerAccumulator.AddError();
    public void RecordProducerQueueTime(double queueTimeMs) =>
        _producerAccumulator.AddQueueTime(queueTimeMs);

    public void RecordConsumerLag(long currentLag) =>
        _consumerAccumulator.UpdateLag(currentLag);

    private GlobalMetricsSnapshot CreateSnapshot()
    {
        var now = DateTime.UtcNow;
        var effectiveTime = _endTime ?? now;
        TimeSpan duration = effectiveTime - _startTime;
        double elapsed = duration.TotalSeconds;

        return new GlobalMetricsSnapshot(
            effectiveTime,
            duration,
            _producerAccumulator.GetSnapshot(elapsed),
            _consumerAccumulator.GetSnapshot(elapsed)
        );
    }

    public void Reset()
    {
        Log.Information("Metrics collection reset. Starting new recording session.");

        _startTime = DateTime.UtcNow;
        _endTime = null;
        _producerAccumulator.Reset();
        _consumerAccumulator.Reset();
    }

    public void Stop()
    {
        if (_endTime == null)
        {
            _endTime = DateTime.UtcNow;
            TimeSpan totalDuration = _endTime.Value - _startTime;
            Log.Information("Metrics collection stopped. Total recording duration: {Duration:N2}s", totalDuration.TotalSeconds);
        }
    }
}
