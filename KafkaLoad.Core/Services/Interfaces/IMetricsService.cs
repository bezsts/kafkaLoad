using System;
using KafkaLoad.Core.Models.Metrics;

namespace KafkaLoad.Core.Services.Interfaces;

public interface IMetricsService
{
    void RecordProducerSuccess(int bytes, double latencyMs);
    void RecordConsumerSuccess(int bytes, double latencyMs);
    void RecordProducerError();
    void RecordConsumerError();
    void RecordProducerInFlightIncrement();
    void RecordProducerInFlightDecrement();
    void RecordProducerQueueTime(double queueTimeMs);
    void RecordConsumerLag(long currentLag);
    void Reset();
    void Stop();

    GlobalMetricsSnapshot CurrentSnapshot { get; }
    IObservable<GlobalMetricsSnapshot> MetricsStream { get; }
}
