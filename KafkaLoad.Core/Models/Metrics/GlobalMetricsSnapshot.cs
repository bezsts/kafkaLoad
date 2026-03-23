using System;

namespace KafkaLoad.Core.Models.Metrics;

public record GlobalMetricsSnapshot(
    DateTime Timestamp,
    TimeSpan Duration,
    ProducerMetricsSnapshot Producer,
    ConsumerMetricsSnapshot Consumer
);
