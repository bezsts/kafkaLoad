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
    public IObservable<GlobalMetricsSnapshot> MetricsStream => 
        Observable.Interval(TimeSpan.FromSeconds(1))
                  .Select(_ => CreateSnapshot());

    public void RecordProducerSuccess(int bytes, double latencyMs) =>
        _producerAccumulator.AddSuccess(bytes, latencyMs);

    // public void RecordProducerSuccess(int bytes, double latencyMs)
    // {
    //     if (_producerAccumulator.GetSnapshot(1).TotalMsgsSent == 0) {
    //         Console.WriteLine("[METRICS-SERVICE] First data received!");
    //     }
    //         _producerAccumulator.AddSuccess(bytes, latencyMs);
    // }
    public void RecordConsumerSuccess(int bytes, double latencyMs) =>
        _consumerAccumulator.AddSuccess(bytes, latencyMs);

    public void RecordProducerError() => _producerAccumulator.AddError();
    public void RecordConsumerError() => _consumerAccumulator.AddError();

    private GlobalMetricsSnapshot CreateSnapshot()
    {
        var now = DateTime.UtcNow;
        TimeSpan duration = now - _startTime;
        double elapsed = duration.TotalSeconds;

        return new GlobalMetricsSnapshot(
            now,
            duration,
            _producerAccumulator.GetSnapshot(elapsed),
            _consumerAccumulator.GetSnapshot(elapsed)
        );
    }

    public void Reset()
    {
        _startTime = DateTime.UtcNow;
        _producerAccumulator.Reset();
        _consumerAccumulator.Reset();    
    }
}
