using System;

namespace KafkaLoad.Desktop.Models;

public class TestReport
{
    public TestScenario TestScenario { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalMsgsSent { get; set; }
    public long TotalBytesSent { get; set; }
    public long SuccessMsgsSent { get; set; }
    public long ErrorMsgsSent { get; set; }
    public double AvgThroughputMsg { get; set; }
    public double AvgThroughputBytes { get; set; } 
    public double AvgLatMs { get; set; }
    public double P95Lat { get; set; }
    public double MaxLat { get; set; }
}
