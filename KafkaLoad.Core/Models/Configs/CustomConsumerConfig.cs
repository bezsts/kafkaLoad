using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models.Configs;
using KafkaLoad.Core.Models.Interfaces;
using System;

namespace KafkaLoad.Core.Models;

public class CustomConsumerConfig : IConfigModel
{
    private const int DefaultFetchMinBytes = 1;
    private const int DefaultFetchMaxBytes = 50 * 1024 * 1024;
    private const int DefaultFetchMaxWaitMs = 500;
    private const int DefaultMaxPollIntervalMs = 5 * 60 * 1000;


    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Unique identifier for the consumer group.
    // Consumers sharing the same GroupId divide the topic partitions among themselves.
    public string GroupId { get; set; } = string.Empty;

    // Determines the action when there is no initial offset in Kafka 
    // or if the current offset does not exist anymore (e.g., data deleted).
    // Earliest: Start from the beginning of the topic.
    // Latest: Start from new messages only.
    public AutoOffsetResetEnum AutoOffsetReset { get; set; }

    // If true, the consumer's offset will be periodically committed in the background.
    // If false, you must manually commit offsets (recommended for high reliability).

    public bool EnableAutoCommit { get; set; } = false;

    // Minimum amount of data the server should return for a fetch request.
    // If data is insufficient, the broker waits before answering.
    // Higher values reduce CPU load on the broker but increase latency.
    public int FetchMinBytes { get; set; } = DefaultFetchMinBytes;
    public int FetchMaxBytes { get; set; } = DefaultFetchMaxBytes;

    // Maximum amount of time the server will block before answering the fetch request
    // if there isn't sufficient data to satisfy FetchMinBytes.
    public int FetchMaxWait { get; set; } = DefaultFetchMaxWaitMs;


    // The maximum delay between invocations of .Consume() or .Poll().
    // If you take longer than this to process messages, the broker will consider 
    // this consumer dead and trigger a rebalance (kicking you out of the group).
    public int MaxPollInterval { get; set; } = DefaultMaxPollIntervalMs;

    public CustomSecurityConfig Security { get; set; } = new CustomSecurityConfig();
    public DateTime CreatedAt { get; set; }
    public override string ToString() => Name;
}