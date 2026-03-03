namespace KafkaLoad.Desktop.Models.Reports
{
    public class TestScenarioConfigSnapshot
    {
        public string TopicName { get; set; } = string.Empty;
        public int ProducersCount { get; set; }
        public int ConsumersCount { get; set; }
        public int MessageSize { get; set; }
        public double? TargetThroughput { get; set; }
    }
}
