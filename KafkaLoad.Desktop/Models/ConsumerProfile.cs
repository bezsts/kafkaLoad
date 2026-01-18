using System;
using Confluent.Kafka;

namespace KafkaLoad.Desktop.Models;

public class ConsumerProfile
{
    public string Name { get; set; } = "Default Consumer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "load-test-group";
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public bool EnableAutoCommit { get; set; } = true;

    public override string ToString() => Name;
}
