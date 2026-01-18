using System;

namespace KafkaLoad.Desktop.Models;

public class TestConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "load-test-topic";
    public int ProducerCount { get; set; } = 1;
    public int ConsumerCount { get; set; } = 0;
    public int MessageSizeBytes { get; set; } = 1024; 
    public int DurationSeconds { get; set; } = 60;
}
