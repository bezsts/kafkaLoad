using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Engine.Workers;
using KafkaLoad.Desktop.Services.Generators;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.Services.Workers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services;

public class ProducerWorker : BaseWorker
{
    private readonly IKafkaProducer<byte[], byte[]> _producer;
    private readonly ThroughputController _throughputController;

    private readonly IDataGenerator _keyGenerator;
    private readonly IDataGenerator _valueGenerator;

    // How often to force a poll to trigger delivery callbacks
    //private const int pollIntervalMessages = 1000;

    public ProducerWorker(
        IKafkaProducer<byte[], byte[]> producer,
        IMetricsService metrics,
        string topic,
        IDataGenerator keyGenerator,
        IDataGenerator valueGenerator,
        ThroughputController throughputController
    ) : base(metrics, topic)
    {
        _producer = producer;
        _keyGenerator = keyGenerator;
        _valueGenerator = valueGenerator;
        _throughputController = throughputController;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        // Yield to ensure the UI thread isn't blocked during startup
        await Task.Yield();

        int iterations = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await _throughputController.WaitForTokenAsync(ct);

                byte[]? key = _keyGenerator.Next();
                byte[]? val = _valueGenerator.Next();

                var stopwatch = Stopwatch.StartNew();

                // Send message asynchronously
                _producer.Produce(Topic, key, val,
                    (deliveryReport) =>
                    {
                        stopwatch.Stop();
                        if (deliveryReport.Error.IsError)
                        {
                            Metrics.RecordProducerError();
                        }
                        else
                        {
                            int bytes = (deliveryReport.Message.Key?.Length ?? 0) +
                                     (deliveryReport.Message.Value?.Length ?? 0);
                            Metrics.RecordProducerSuccess(bytes, stopwatch.Elapsed.TotalMilliseconds);
                        }
                    });

                // Periodically poll to process delivery reports
                if (++iterations % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[WORKER] Producer cancelled cleanly.");
        }
        //catch (ProduceException<byte[], byte[]> e)
        //{

        //    // If the internal queue is full, wait a bit for the driver to catch up
        //    if (e.Error.Code == ErrorCode.Local_QueueFull)
        //    {
        //        _producer.Poll(TimeSpan.FromMilliseconds(10));
        //    }
        //    else
        //    {
        //        //TODO: log kafka error 
        //        Metrics.RecordProducerError();
        //    }
        //}
        catch (Exception ex)
        {
            Debug.WriteLine($"[WORKER ERROR] {ex.Message}");
        }
        finally
        {
            try
            {
                _producer.Flush(2);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[WORKER FLUSH ERROR] {e.Message}");
            }
        }
    }
}
