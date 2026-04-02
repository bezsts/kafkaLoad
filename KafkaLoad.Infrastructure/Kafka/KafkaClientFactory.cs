using Confluent.Kafka;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Configs;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;

namespace KafkaLoad.Infrastructure.Kafka;

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
            MaxInFlight = config.MaxInFlightRequestsPerConnection,

            QueueBufferingMaxMessages = 1_000_000
        };

        ApplySecurityConfig(producerConfig, config.Security);

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

        ApplySecurityConfig(consumerConfig, config.Security);

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

    private void ApplySecurityConfig(ClientConfig clientConfig, CustomSecurityConfig securityConfig)
    {
        if (securityConfig == null) return;

        clientConfig.SecurityProtocol = (SecurityProtocol)securityConfig.SecurityProtocol;

        // Map SASL Authentication
        if (clientConfig.SecurityProtocol == SecurityProtocol.SaslPlaintext ||
            clientConfig.SecurityProtocol == SecurityProtocol.SaslSsl)
        {
            clientConfig.SaslMechanism = (SaslMechanism)securityConfig.SaslMechanism;

            if (!string.IsNullOrWhiteSpace(securityConfig.SaslUsername))
                clientConfig.SaslUsername = securityConfig.SaslUsername;

            if (!string.IsNullOrWhiteSpace(securityConfig.SaslPassword))
                clientConfig.SaslPassword = securityConfig.SaslPassword;
        }

        // Map SSL Certificates
        if (clientConfig.SecurityProtocol == SecurityProtocol.Ssl ||
            clientConfig.SecurityProtocol == SecurityProtocol.SaslSsl)
        {
            if (!string.IsNullOrWhiteSpace(securityConfig.SslCaLocation))
                clientConfig.SslCaLocation = securityConfig.SslCaLocation;

            if (!string.IsNullOrWhiteSpace(securityConfig.SslCertificateLocation) &&
                !string.IsNullOrWhiteSpace(securityConfig.SslKeyLocation))
            {
                clientConfig.SslCertificateLocation = securityConfig.SslCertificateLocation;
                clientConfig.SslKeyLocation = securityConfig.SslKeyLocation;

                if (!string.IsNullOrWhiteSpace(securityConfig.SslKeyPassword))
                    clientConfig.SslKeyPassword = securityConfig.SslKeyPassword;
            }
        }
    }
}
