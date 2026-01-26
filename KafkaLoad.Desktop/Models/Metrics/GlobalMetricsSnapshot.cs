using System;

namespace KafkaLoad.Desktop.Models;

public record GlobalMetricsSnapshot(
    DateTime Timestamp,
    ProducerMetricsSnapshot Producer,
    ConsumerMetricsSnapshot Consumer
);
