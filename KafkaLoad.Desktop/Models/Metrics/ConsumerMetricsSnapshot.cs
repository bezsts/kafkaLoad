namespace KafkaLoad.Desktop.Models;

public record ConsumerMetricsSnapshot(
    long TotalMsgConsumed,
    long TotalBytesConsumed,
    long SuccessMsgsConsumed,
    long ErrorMsgsConsumed,
    double ThroughputMsg,
    double ThroughputBytes,
    double AvgLatencyMs
);