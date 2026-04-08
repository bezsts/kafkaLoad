using Confluent.Kafka;
using KafkaLoad.Core.Models;

namespace KafkaLoad.Core.Services.Interfaces;

public interface IKafkaClientFactory
{
    IProducer<TKey, TValue> CreateProducer<TKey, TValue>(CustomProducerConfig config, string bootstrapServers, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer);

    IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(CustomConsumerConfig config, string bootstrapServers, IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer);
}