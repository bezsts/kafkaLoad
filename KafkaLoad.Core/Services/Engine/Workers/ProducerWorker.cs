using Confluent.Kafka;
using KafkaLoad.Core.Services.Generators;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Engine.Workers;

public class ProducerWorker : BaseWorker
{
    private readonly IProducer<byte[], byte[]> _producer;
    private readonly ThroughputController _throughputController;

    private readonly IDataGenerator _keyGenerator;
    private readonly IDataGenerator _valueGenerator;

    // How often to force a poll to trigger delivery callbacks
    //private const int pollIntervalMessages = 1000;

    public ProducerWorker(
        IProducer<byte[], byte[]> producer,
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
            Log.Information("ProducerWorker starting on topic: {Topic}", Topic);

            while (!ct.IsCancellationRequested)
            {
                await _throughputController.WaitForTokenAsync(ct);

                byte[]? key = _keyGenerator.Next();
                byte[]? val = _valueGenerator.Next();

                var stopwatch = Stopwatch.StartNew();

                var kafkaMessage = new Message<byte[], byte[]>
                {
                    Key = key,
                    Value = val
                };

                // Send message asynchronously
                _producer.Produce(Topic, kafkaMessage,
                    (deliveryReport) =>
                    {
                        stopwatch.Stop();
                        if (deliveryReport.Error.IsError)
                        {
                            Log.Warning("Message delivery failed. Error: {Reason} (Code: {Code})", deliveryReport.Error.Reason, deliveryReport.Error.Code);
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
            Log.Information("ProducerWorker cancelled cleanly on topic: {Topic}", Topic);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in ProducerWorker loop on topic: {Topic}", Topic);
        }
        finally
        {
            try
            {
                Log.Debug("Flushing producer buffer for topic: {Topic}", Topic);
                _producer.Flush(TimeSpan.FromSeconds(2));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occurred while flushing ProducerWorker on topic: {Topic}", Topic);
            }

            Log.Information("ProducerWorker stopped on topic: {Topic}", Topic);
        }
    }
}
