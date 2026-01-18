using System;

namespace KafkaLoad.Desktop.Models;

public class TestResult
{
    public int Id { get; set; }
    public DateTime RunDate { get; set; }
    public TestConfiguration Configuration { get; set; } = new();

    public double AvgThroughput { get; set; }
    public double MaxLatency { get; set; }
    public long TotalMessages { get; set; }
}
