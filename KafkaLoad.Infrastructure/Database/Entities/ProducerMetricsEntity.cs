namespace KafkaLoad.Infrastructure.Database.Entities;

public class ProducerMetricsEntity
{
    public int Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public TestReportEntity Report { get; set; } = null!;

    public long TotalAttempted { get; set; }
    public long SuccessSent { get; set; }
    public long ErrorCount { get; set; }
    public long TotalBytesSent { get; set; }
    public double ErrorRatePercent { get; set; }
    public double ThroughputMsgSec { get; set; }
    public double ThroughputBytesSec { get; set; }
    public double AvgLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
}
