using System;
using System.Reactive.Linq;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

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
        }
    }
}
