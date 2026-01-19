using System;
using Confluent.Kafka;

namespace KafkaLoad.Desktop.Models;

public class ProducerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = String.Empty;
    public override string ToString() => Name;
}
