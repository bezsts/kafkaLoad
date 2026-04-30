namespace KafkaLoad.Infrastructure.Database.Entities;

public class ConsumerMetricsEntity
{
    public int Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public TestReportEntity Report { get; set; } = null!;

    public long TotalConsumed { get; set; }
    public long SuccessConsumed { get; set; }
    public long ErrorCount { get; set; }
    public long TotalBytesConsumed { get; set; }
    public double ThroughputMsgSec { get; set; }
    public double ThroughputBytesSec { get; set; }
    public double MaxE2ELatencyMs { get; set; }
    public double P50E2ELatencyMs { get; set; }
    public double P95E2ELatencyMs { get; set; }
    public double P99E2ELatencyMs { get; set; }
    public long MaxConsumerLag { get; set; }
    public long FinalConsumerLag { get; set; }
}
