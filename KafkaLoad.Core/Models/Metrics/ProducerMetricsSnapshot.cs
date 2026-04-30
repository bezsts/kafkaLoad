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

    double AvgLatencyMs,
    double MaxLatencyMs,

    double P95Lat,

    long LatencySumMs
)
{
    public long TotalErrors => ErrorMessages;
};
