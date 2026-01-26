using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class ProducerWorker
{
    private readonly IProducer<byte[], byte[]> _producer; //TODO: implement generic producer
    private readonly IMetricsService _metrics;
    private readonly string _topic;
    private readonly byte[] _payload; //TODO: implement generation messages

    public ProducerWorker(
        IProducer<byte[], byte[]> producer,
        IMetricsService metrics,
        string topic,
        int messageSize
    )
    {
        _producer = producer;
        _metrics = metrics;
        _topic = topic;

        _payload = new byte[messageSize]; 
        new Random().NextBytes(_payload);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await Task.Yield(); 

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                _producer.Produce(_topic, new Message<byte[], byte[]> { Value = _payload },
                    (deliveryReport) =>
                    {
                        stopwatch.Stop();
                        if (deliveryReport.Error.IsError)
                        {
                            _metrics.RecordProducerError();
                        }
                        else
                        {
                            _metrics.RecordProducerSuccess(_payload.Length, stopwatch.Elapsed.TotalMilliseconds);
                        }
                    });
                    
                //TODO: add delay to limit speed (cap algorithm?) 
            }
            catch (Exception)
            {
                _metrics.RecordProducerError();
            }
        }
    }
}
