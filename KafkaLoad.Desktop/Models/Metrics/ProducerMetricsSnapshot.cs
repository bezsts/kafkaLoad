namespace KafkaLoad.Desktop.Models;

public record ProducerMetricsSnapshot(
    long TotalMessagesAttempted,
    long SuccessMessagesSent,
    long ErrorMessages,
    long TotalBytesSent,

    double ErrorRatePercent,

    double ThroughputMsgSec,
    double ThroughputBytesSec,
    
    double AvgLatencyMs,
    double MaxLatencyMs,

    double P95Lat
);
