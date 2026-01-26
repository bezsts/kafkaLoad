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
    IObservable<GlobalMetricsSnapshot> MetricsStream { get; }
}
