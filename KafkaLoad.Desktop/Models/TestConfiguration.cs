using System;

namespace KafkaLoad.Desktop.Models;

public class TestConfiguration
{
    public string Topic { get; set; } = "load-test-topic";
    public ProducerProfile ProducerSettings { get; set; } = new();
    public int ProducerCount { get; set; } = 0;
    public int MessageSizeBytes { get; set; } = 1024; 
    public int DurationSeconds { get; set; } = 60;
}
