using Confluent.Kafka;
using KafkaLoad.Core.Models;

namespace KafkaLoad.Core.Services.Interfaces;

public interface IKafkaClientFactory
{
    // Creates a producer based on configuration model
    IProducer<TKey, TValue> CreateProducer<TKey, TValue>(CustomProducerConfig config, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer);

    // Creates a consumer based on configuration model
    IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(CustomConsumerConfig config, IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer);
}