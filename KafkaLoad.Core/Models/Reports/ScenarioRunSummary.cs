using System;

namespace KafkaLoad.Core.Models.Reports
{
    public class ScenarioRunSummary
    {
        public string ScenarioName { get; set; } = string.Empty;
        public int RunCount { get; set; }
        public DateTime LastRun { get; set; }
        public double AvgThroughputMsgSec { get; set; }
    }
}
