using KafkaLoad.Desktop.Enums;
using System;
using System.Collections.Generic;

namespace KafkaLoad.Desktop.Models.Reports
{
    public class TestReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ScenarioName { get; set; } = string.Empty;
        public TestType TestType { get; set; }
        public int DurationSeconds { get; set; }

        public long TotalMessagesSent { get; set; }
        public long TotalErrors { get; set; }

        public Dictionary<string, double> ExtendedMetrics { get; set; } = new();
        public TestScenarioConfigSnapshot ConfigSnapshot { get; set; } = new();

        public Dictionary<string, List<TimeSeriesPoint>> TimeSeriesData { get; set; } = new();
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
