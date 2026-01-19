using System;

namespace KafkaLoad.Desktop.Models;

public class TestScenario
{
    public string Name { get; set;} = string.Empty;
    public ProducerConfiguration ProducerConfiguration { get; set; } = new();
    public ConsumerConfiguration ConsumerConfiguration { get; set; } = new();
    public string TopicName { get; set; } = string.Empty;
    public int ProducerCount { get; set; } = 0;
    public int ConsumerCount { get; set; } = 0;
    public int MessageSize { get; set; } = 1024; 
    public int Duration { get; set; } = 60;
}
