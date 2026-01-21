using System;
using Confluent.Kafka;
using KafkaLoad.Desktop.Enums;

namespace KafkaLoad.Desktop.Models;

public class ConsumerConfiguration
{
    private const int DefaultFetchMinBytes = 1;
    private const int DefaultFetchMaxWaitMs = 500;
    private const int DefaultMaxPollRecords = 500;
    private const int DefaultMaxPollIntervalMs = 5 * 60 * 1000;


    public string Name { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;

    // Unique identifier for the consumer group.
    // Consumers sharing the same GroupId divide the topic partitions among themselves.
    public string GroupId { get; set; } = string.Empty;
    public KeyDeserializerEnum KeyDeserializer { get; set; }
    public ValueDeserializerEnum ValueDeserializer { get; set; }

    // Determines the action when there is no initial offset in Kafka 
    // or if the current offset does not exist anymore (e.g., data deleted).
    // Earliest: Start from the beginning of the topic.
    // Latest: Start from new messages only.
    public AutoOffsetResetEnum AutoOffsetReset { get; set; }

    // If true, the consumer's offset will be periodically committed in the background.
    // If false, you must manually commit offsets (recommended for high reliability).
    public bool EnableAutoCommit { get; set; } = true;

    // Minimum amount of data the server should return for a fetch request.
    // If data is insufficient, the broker waits before answering.
    // Higher values reduce CPU load on the broker but increase latency.
    public int FetchMinBytes { get; set; } = DefaultFetchMinBytes;

    // Maximum amount of time the server will block before answering the fetch request
    // if there isn't sufficient data to satisfy FetchMinBytes.
    public int FetchMaxWait { get; set; } = DefaultFetchMaxWaitMs;

    // The maximum number of records returned in a single call to .Consume() or .Poll().
    // Use this to control the batch size of processing.
    public int MaxPollRecords { get; set; } = DefaultMaxPollRecords;

    // The maximum delay between invocations of .Consume() or .Poll().
    // If you take longer than this to process messages, the broker will consider 
    // this consumer dead and trigger a rebalance (kicking you out of the group).
    public int MaxPollInterval { get; set; } = DefaultMaxPollIntervalMs;
    public override string ToString() => Name;
}