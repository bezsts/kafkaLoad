using System;
using System.Reactive.Linq;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class MetricsService : IMetricsService
{
    private readonly MetricsAccumulator _accumulator = new();
    public IObservable<LiveMetrics> MetricsStream => 
        Observable.Interval(TimeSpan.FromSeconds(1))
                  .Select(_ => _accumulator.GetSnapshot());
                  
    public void RecordProducerSuccess(int bytes, double latencyMs) =>
        _accumulator.AddSuccess(bytes, latencyMs);

    public void RecordError() => _accumulator.AddError();

    public void Reset() => _accumulator.Reset();
}
