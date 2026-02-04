using System;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class KafkaProducer<TKey,TValue> : IKafkaProducer<TKey, TValue>
{
    private readonly IProducer<TKey, TValue> _producer;

    public KafkaProducer(IProducer<TKey, TValue> producer)
    {
        _producer = producer;
    }

    public void Produce(string topic, TKey key, TValue message, Action<DeliveryReport<TKey, TValue>> deliveryHandler)
    {
        _producer.Produce(topic, new Message<TKey, TValue> 
        { 
            Key = key, 
            Value = message 
        }, deliveryHandler);
    }
    public void Poll(TimeSpan timeout)
    {
        _producer.Poll(timeout);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
