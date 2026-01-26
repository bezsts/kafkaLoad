using System;

namespace KafkaLoad.Desktop.Models;

public record GlobalMetricsSnapshot(
    DateTime Timestamp,
    TimeSpan Duration,
    ProducerMetricsSnapshot Producer,
    ConsumerMetricsSnapshot Consumer
);
