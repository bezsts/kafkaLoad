using System;
using Confluent.Kafka;

namespace KafkaLoad.Desktop.Models;

public class ProducerProfile
{
    public string Name { get; set; } = "Default Producer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public Acks Acks { get; set; } = Acks.Leader;
    public double LingerMs { get; set; } = 5;
    public int BatchSize { get; set; } = 65536; // 64KB
    public CompressionType Compression { get; set; } = CompressionType.None;

    public override string ToString() => Name;
}
