using System;

namespace KafkaLoad.Desktop.Models;

public record RealTimeMetric(
    DateTime Timestamp, 
    double ThroughputRps, 
    double AverageLatencyMs,
    long TotalMessagesSent
);
