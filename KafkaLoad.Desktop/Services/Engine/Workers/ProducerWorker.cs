using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class ProducerWorker
{
    private readonly IKafkaProducer<byte[], byte[]> _producer;
    private readonly IMetricsService _metrics;
    private readonly string _topic;
    private readonly byte[] _payload; //TODO: implement generation messages
    private int pollCounter = 0;

    public ProducerWorker(
        IKafkaProducer<byte[], byte[]> producer,
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

                _producer.Produce(_topic, null, _payload,
                    (deliveryReport) =>
                    {
                        stopwatch.Stop();
                        if (deliveryReport.Error.IsError)
                        {
                            _metrics.RecordProducerError();
                        }
                        else
                        {
                            // if (new Random().Next(100) == 0) 
                            // {
                            //     Console.WriteLine($"[WORKER] Success! Bytes: {_payload.Length}");
                            // }
                            _metrics.RecordProducerSuccess(_payload.Length, stopwatch.Elapsed.TotalMilliseconds);
                        }
                    });

                pollCounter++;
                if (pollCounter % 1000 == 0)
                {
                    _producer.Poll(TimeSpan.Zero); 
                }
            }
            catch (ProduceException<byte[], byte[]> e)
            {
                if (e.Error.Code == ErrorCode.Local_QueueFull)
                {
                    _producer.Poll(TimeSpan.FromMilliseconds(10));
                }
                else
                {
                    Console.WriteLine($"Kafka Error: {e.Error.Reason}");
                    _metrics.RecordProducerError();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical Worker Error: " + ex.Message);
                _metrics.RecordProducerError();
                await Task.Delay(100, ct); 
            }
        }
    }
}
