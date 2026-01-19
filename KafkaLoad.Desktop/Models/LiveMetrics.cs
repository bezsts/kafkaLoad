using System;

namespace KafkaLoad.Desktop.Models;

public record LiveMetrics(
    DateTime Timestamp, 
    long TotalMsgsSent,
    long TotalBytesSent,
    long SuccessMsgsSent,
    long ErrorMsgsSent,
    double ThroughputMsg,
    double ThroughputBytes, 
    double AvgLatMs,
    double P95Lat
);
