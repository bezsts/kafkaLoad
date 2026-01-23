using System;
using Confluent.Kafka;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IKafkaClientFactory
{
    // Creates a producer based on configuration model
    IProducer<TKey, TValue> CreateProducer<TKey, TValue>(Models.CustomProducerConfig config, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer);

    // Creates a consumer based on configuration model
    IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(Models.CustomConsumerConfig config, IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer);
}
