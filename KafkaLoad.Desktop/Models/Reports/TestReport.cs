using KafkaLoad.Desktop.Enums;
using System;
using System.Collections.Generic;

namespace KafkaLoad.Desktop.Models.Reports
{
    public class TestReport
    {
        // --- METADATA ---
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ScenarioName { get; set; } = string.Empty;
        public TestType TestType { get; set; }
        public int DurationSeconds { get; set; }

        // --- CONFIGURATION SNAPSHOT ---
        public TestScenarioConfigSnapshot ConfigSnapshot { get; set; } = new();

        // --- EXPLICIT METRICS ---
        public ProducerReportMetrics ProducerMetrics { get; set; } = new();
        public ConsumerReportMetrics? ConsumerMetrics { get; set; }

        // --- GRAPH DATA ---
        public Dictionary<string, List<TimeSeriesPoint>> TimeSeriesData { get; set; } = new();
    }

    public class ProducerReportMetrics
    {
        public long TotalMessagesAttempted { get; set; }
        public long SuccessMessagesSent { get; set; }
        public long ErrorMessages { get; set; }
        public long TotalBytesSent { get; set; }

        public double ErrorRatePercent { get; set; }

        public double ThroughputMsgSec { get; set; }
        public double ThroughputBytesSec { get; set; }

        public double AvgLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double P95Lat { get; set; }
    }

    public class ConsumerReportMetrics
    {
        public long TotalMessagesConsumed { get; set; }
        public long TotalBytesConsumed { get; set; }
        public long SuccessMessagesConsumed { get; set; }
        public long ErrorMessagesConsumed { get; set; }

        public double ThroughputMsgSec { get; set; }
        public double ThroughputBytesSec { get; set; }

        public double AvgEndToEndLatencyMs { get; set; }
        public double MaxEndToEndLatencyMs { get; set; }

        public long MaxConsumerLag { get; set; }
        public long FinalConsumerLag { get; set; }
    }
    public class TestScenarioConfigSnapshot
    {
        public string TopicName { get; set; } = string.Empty;
        public int ProducersCount { get; set; }
        public int ConsumersCount { get; set; }
        public int MessageSize { get; set; }
        public double? TargetThroughput { get; set; }
    }

    public class TimeSeriesPoint
    {
        public double TimeSeconds { get; set; }
        public double Value { get; set; }

        public TimeSeriesPoint()
        {
        }

        public TimeSeriesPoint(double time, double value)
        {
            TimeSeconds = Math.Round(time, 2);
            Value = Math.Round(value, 2);
        }
    }
}
