namespace KafkaLoad.Desktop.Models;

public record ConsumerMetricsSnapshot(
    long TotalMessagesConsumed,
    long TotalBytesConsumed,
    long SuccessMessagesConsumed,
    long ErrorMessagesConsumed,

    double ThroughputMsgSec,
    double ThroughputBytesSec,

    double AvgEndToEndLatencyMs,
    double MaxEndToEndLatencyMs,

    long MaxConsumerLag,
    long FinalConsumerLag
);