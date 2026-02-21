using System;
using Confluent.Kafka;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IKafkaProducer<TKey, TValue> : IDisposable
{
    void Produce(string topic, TKey key, TValue message, Action<DeliveryReport<TKey, TValue>> deliveryHandler);

    void Poll(TimeSpan timeout);
    void Flush(int seconds);
}
