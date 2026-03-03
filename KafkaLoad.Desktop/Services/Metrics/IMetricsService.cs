using System;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IMetricsService
{
    void RecordProducerSuccess(int bytes, double latencyMs);
    void RecordConsumerSuccess(int bytes, double latencyMs);
    void RecordProducerError();
    void RecordConsumerError();
    void Reset();
    void Stop();

    GlobalMetricsSnapshot CurrentSnapshot { get; }
    IObservable<GlobalMetricsSnapshot> MetricsStream { get; }
}
