using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models.Configs;
using KafkaLoad.Core.Models.Interfaces;

namespace KafkaLoad.Core.Models;

public class CustomProducerConfig : IConfigModel
{
    private const int DefaultBatchSizeBytes = 16 * 1024;
    private const long DefaultBufferMemoryBytes = 32 * 1024 * 1024;
    private const int DefaultLingerMs = 0;
    private const int DefaultMaxInFlightRequests = 5;
    public string Name { get; set; } = string.Empty;

    // Client identifier. Used to track the source of requests in broker logs and metrics.
    public string ClientID { get; set; } = string.Empty;

    // Determines how many brokers must acknowledge the message receipt.
    // None - Producer does not wait for any acknowledgment.
    // Leader - Only the partition leader must acknowledge.
    // All - The leader and all in-sync replicas must acknowledge.
    public AcksEnum Acks { get; set; }

    // The maximum number of retries the producer will attempt in case of transient errors.
    public int Retries { get; set; } = int.MaxValue;

    // Ensures exactly-once delivery semantics (no duplicates). 
    // Requires Acks = All.
    public bool EnableIdempotence { get; set; }

    // Maximum size (in bytes) of a single batch of messages.
    // Larger values reduce the number of requests but increase latency.
    public int BatchSize { get; set; } = DefaultBatchSizeBytes;

    // Wait time (in milliseconds) before sending a batch.
    // Allows accumulating more messages in a single batch.
    // Higher values increase throughput but add latency.
    public double Linger { get; set; } = DefaultLingerMs;

    public CompressionTypeEnum CompressionType { get; set; }

    // Total memory size (in bytes) available to the producer for buffering.
    // If the buffer is full, the .Produce() call will block.
    public long BufferMemory { get; set; } = DefaultBufferMemoryBytes;

    // Maximum number of unacknowledged requests that can be sent per connection.
    // Higher values increase throughput but can risk message reordering if retries occur.
    public int MaxInFlightRequestsPerConnection { get; set; } = DefaultMaxInFlightRequests;

    public bool AutoCreateTopicsEnable { get; } = false;
    public CustomSecurityConfig Security { get; set; } = new CustomSecurityConfig();
    public override string ToString() => Name;
}
