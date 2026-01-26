namespace KafkaLoad.Desktop.Models;

public record ProducerMetricsSnapshot(
    long TotalMsgsSent,
    long TotalBytesSent,
    long SuccessMsgsSent,
    long ErrorMsgsSent,
    double ThroughputMsg,
    double ThroughputBytes, 
    double AvgLatMs,
    double P95Lat
);
