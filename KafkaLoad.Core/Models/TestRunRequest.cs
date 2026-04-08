namespace KafkaLoad.Core.Models;

public class TestRunRequest
{
    public TestScenario Scenario { get; init; } = null!;
    public string BootstrapServers { get; init; } = string.Empty;
    public string TopicName { get; init; } = string.Empty;
}
