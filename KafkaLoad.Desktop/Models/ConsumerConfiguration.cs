using System;
using Confluent.Kafka;

namespace KafkaLoad.Desktop.Models;

public class ConsumerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;
    public override string ToString() => Name;
}