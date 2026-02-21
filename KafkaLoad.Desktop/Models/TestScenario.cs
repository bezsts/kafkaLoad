using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models.Interfaces;
using System;

namespace KafkaLoad.Desktop.Models;

public class TestScenario : IConfigModel
{
    public string Name { get; set;} = string.Empty;
    public CustomProducerConfig? ProducerConfig { get; set; }
    public CustomConsumerConfig? ConsumerConfig { get; set; }
    public KeyGenerationStrategy KeyStrategy { get; set; } = KeyGenerationStrategy.RandomString;
    public ValueGenerationStrategy ValueStrategy { get; set; } = ValueGenerationStrategy.RandomString;
    public TestType TestType { get; set; } = TestType.Load;
    public string? FixedTemplate { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int? ProducerCount { get; set; } = 1;
    public int? ConsumerCount { get; set; } = 1;
    public int? MessageSize { get; set; } = 1024; 
    public int? Duration { get; set; } = 60;
    public int? TargetThroughput { get; set; }
    public int? BaseThroughput { get; set; }
    public int? SpikeThroughput { get; set; }
}
