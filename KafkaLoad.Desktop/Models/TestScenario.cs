using System;

namespace KafkaLoad.Desktop.Models;

public class TestScenario
{
    public string Name { get; set;} = string.Empty;
    public CustomProducerConfig ProducerConfig { get; set; } = new();
    public CustomConsumerConfig ConsumerConfig { get; set; } = new();
    public string TopicName { get; set; } = string.Empty;
    public int ProducerCount { get; set; } = 1;
    public int ConsumerCount { get; set; } = 1;
    public int MessageSize { get; set; } = 1024; 
    public int Duration { get; set; } = 60;
}
