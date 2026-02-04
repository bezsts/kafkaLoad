using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class ConsumerWorker
{
    private readonly IConsumer<byte[], byte[]> _consumer;
    private readonly IMetricsService _metrics;
    private readonly string _topic;

    public ConsumerWorker(
        IConsumer<byte[], byte[]> consumer,
        IMetricsService metrics,
        string topic)
    {
        _consumer = consumer;
        _metrics = metrics;
        _topic = topic;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await Task.Yield();

        try
        {
            _consumer.Subscribe(_topic);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(ct);

                    if (result != null && !result.IsPartitionEOF)
                    {
                        double latencyMs = (DateTime.UtcNow - result.Message.Timestamp.UtcDateTime).TotalMilliseconds;
                        
                        if (latencyMs < 0) latencyMs = 0;

                        int bytes = result.Message.Value?.Length ?? 0;
                        _metrics.RecordConsumerSuccess(bytes, latencyMs);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException)
                {
                    _metrics.RecordConsumerError();
                }
                catch (Exception)
                {
                    //TODO: add log here
                    _metrics.RecordConsumerError();
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }
}
