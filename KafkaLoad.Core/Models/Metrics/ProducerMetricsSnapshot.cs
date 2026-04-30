namespace KafkaLoad.Core.Models.Metrics;

public record ProducerMetricsSnapshot(
    long TotalMessagesAttempted,
    long SuccessMessagesSent,
    long ErrorMessages,
    long InFlightMessages,
    long TotalBytesSent,

    double ErrorRatePercent,

    double ThroughputMsgSec,
    double ThroughputBytesSec,

    double MaxLatencyMs,

    double P50Lat,
    double P95Lat,
    double P99Lat
)
{
    public long TotalErrors => ErrorMessages;
};
