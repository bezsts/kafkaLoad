namespace KafkaLoad.Core.Models.Metrics;

public record ConsumerMetricsSnapshot(
    long TotalMessagesConsumed,
    long TotalBytesConsumed,
    long SuccessMessagesConsumed,
    long ErrorMessagesConsumed,

    double ThroughputMsgSec,
    double ThroughputBytesSec,

    double MaxEndToEndLatencyMs,
    double P50Lat,
    double P95Lat,
    double P99Lat,

    long MaxConsumerLag,
    long FinalConsumerLag
);
