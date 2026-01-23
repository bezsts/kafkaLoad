using System;
using Confluent.Kafka;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class KafkaClientFactory : IKafkaClientFactory
{
    public IProducer<TKey, TValue> CreateProducer<TKey, TValue>(
        Models.CustomProducerConfig config, 
        ISerializer<TKey> keySerializer, 
        ISerializer<TValue> valueSerializer)
    {
        // 1. Map ViewModel config to Confluent's ProducerConfig
        var producerConfig = new Confluent.Kafka.ProducerConfig
        {
            BootstrapServers = config.BootstrapServers,
            ClientId = config.ClientID,
            
            // Mapping Enums
            Acks = (Acks)config.Acks, 
            CompressionType = (CompressionType)config.CompressionType,
            
            // Numeric settings
            BatchSize = config.BatchSize,
            LingerMs = config.Linger,
            QueueBufferingMaxKbytes = (int)(config.BufferMemory / 1024),
            MessageSendMaxRetries = config.Retries,
            EnableIdempotence = config.EnableIdempotence,
            MaxInFlight = config.MaxInFlightRequestsPerConnection
        };

        // Build the producer
        return new ProducerBuilder<TKey, TValue>(producerConfig)
            .SetKeySerializer(keySerializer)
            .SetValueSerializer(valueSerializer)
            .SetErrorHandler((_, e) => 
            {
                // TODO: Connect this to your logging service
                Console.WriteLine($"Error: {e.Reason}");
            })
            .Build();
    }

    public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(
        Models.CustomConsumerConfig config, 
        IDeserializer<TKey> keyDeserializer, 
        IDeserializer<TValue> valueDeserializer)
    {
        // Map ViewModel config to Confluent's ConsumerConfig
        var consumerConfig = new Confluent.Kafka.ConsumerConfig
        {
            BootstrapServers = config.BootstrapServers,
            GroupId = config.GroupId,
            AutoOffsetReset = (AutoOffsetReset)config.AutoOffsetReset,
            
            // Performance settings
            FetchMinBytes = config.FetchMinBytes,
            FetchMaxBytes = config.FetchMaxBytes,
            FetchWaitMaxMs = config.FetchMaxWait,
            MaxPollIntervalMs = config.MaxPollInterval,

            EnableAutoCommit = true 
        };

        // Build the consumer
        return new ConsumerBuilder<TKey, TValue>(consumerConfig)
            .SetKeyDeserializer(keyDeserializer)
            .SetValueDeserializer(valueDeserializer)
            .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
            .Build();
    }
}
