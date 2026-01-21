using System;
using Confluent.Kafka;
using KafkaLoad.Desktop.Enums;

namespace KafkaLoad.Desktop.Models;

public class ProducerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;

    // Client identifier. Used to track the source of requests in broker logs and metrics.
    public string ClientID { get; set; } = string.Empty;

    public KeySerializerEnum KeySerializer { get; set; }
    public ValueSerializerEnum ValueSerializer { get; set; }
    
    // Determines how many brokers must acknowledge the message receipt.
    // None - Producer does not wait for any acknowledgment.
    // Leader - Only the partition leader must acknowledge.
    // All - The leader and all in-sync replicas must acknowledge.
    public AcksEnum Acks { get; set; }

    // The maximum number of retries the producer will attempt in case of transient errors.
    public int Retries { get; set; }

    // Ensures exactly-once delivery semantics (no duplicates). 
    // Requires Acks = All.
    public bool EnableIdempotence { get; set; }

    // Maximum size (in bytes) of a single batch of messages.
    // Larger values reduce the number of requests but increase latency.
    public int BatchSize { get; set;}

    // Wait time (in milliseconds) before sending a batch.
    // Allows accumulating more messages in a single batch.
    // Higher values increase throughput but add latency.
    public double Linger { get; set; }

    public CompressionTypeEnum CompressionType { get; set; }

    // Total memory size (in bytes) available to the producer for buffering.
    // If the buffer is full, the .Produce() call will block.
    public long BufferMemory { get; set; }

    // Maximum number of unacknowledged requests that can be sent per connection.
    // Higher values increase throughput but can risk message reordering if retries occur.
    public int MaxInFlightRequestsPerConnection { get; set; }

    public bool AutoCreateTopicsEnable { get; } = false;
    public override string ToString() => Name;
}
