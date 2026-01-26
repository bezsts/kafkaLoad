using System;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IMetricsService
{
    void RecordProducerSuccess(int bytes, double latencyMs);
    void RecordError();
    void Reset();
    IObservable<LiveMetrics> MetricsStream { get; }
}
