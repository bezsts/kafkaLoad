using Confluent.Kafka;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using Serilog;
using System;

namespace KafkaLoad.Desktop.Services;

public class KafkaClientFactory : IKafkaClientFactory
{
    public IProducer<TKey, TValue> CreateProducer<TKey, TValue>(
        CustomProducerConfig config, 
        ISerializer<TKey> keySerializer, 
        ISerializer<TValue> valueSerializer)
    {
        Log.Information("Creating Kafka Producer for BootstrapServers: {Servers}, ClientId: {ClientId}", config.BootstrapServers, config.ClientID);

        // 1. Map ViewModel config to Confluent's ProducerConfig
        var producerConfig = new ProducerConfig
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
                Log.Error("Kafka Producer internal error: {Reason} (Code: {Code}, IsFatal: {IsFatal})", e.Reason, e.Code, e.IsFatal);
            })
            .SetLogHandler((_, logMessage) =>
            {
                Log.Debug("librdkafka [Producer]: {Message}", logMessage.Message);
            })
            .Build();
    }

    public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(
        CustomConsumerConfig config, 
        IDeserializer<TKey> keyDeserializer, 
        IDeserializer<TValue> valueDeserializer)
    {
        Log.Information("Creating Kafka Consumer for BootstrapServers: {Servers}, GroupId: {GroupId}", config.BootstrapServers, config.GroupId);

        // Map ViewModel config to Confluent's ConsumerConfig
        var consumerConfig = new ConsumerConfig
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
            .SetErrorHandler((_, e) =>
            {
                Log.Error("Kafka Consumer internal error: {Reason} (Code: {Code}, IsFatal: {IsFatal})", e.Reason, e.Code, e.IsFatal);
            })
            .SetLogHandler((_, logMessage) =>
            {
                Log.Debug("librdkafka [Consumer]: {Message}", logMessage.Message);
            })
            .Build();
    }
}
