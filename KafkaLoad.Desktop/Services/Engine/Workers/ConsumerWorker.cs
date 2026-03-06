using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Engine.Workers;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

//TODO make consumerworker generic
public class ConsumerWorker : BaseWorker
{
    private readonly IConsumer<byte[], byte[]> _consumer;

    public ConsumerWorker(
        IConsumer<byte[], byte[]> consumer,
        IMetricsService metrics,
        string topic) : base(metrics, topic)
    {
        _consumer = consumer;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        await Task.Yield();
        int iterations = 0;

        try
        {
            _consumer.Subscribe(Topic);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Consume blocks until a message is available or the token is cancelled
                    var result = _consumer.Consume(ct);

                    if (result != null && !result.IsPartitionEOF)
                    {
                        // Calculate End-to-End latency
                        double latencyMs = (DateTime.UtcNow - result.Message.Timestamp.UtcDateTime).TotalMilliseconds;

                        // Sanity check for clock skew
                        if (latencyMs < 0) latencyMs = 0;

                        int bytes = result.Message.Value?.Length ?? 0;
                        Metrics.RecordConsumerSuccess(bytes, latencyMs);

                        if (++iterations % 100 == 0)
                        {
                            var watermark = _consumer.GetWatermarkOffsets(result.TopicPartition);
                            if (watermark != null)
                            {
                                long lag = watermark.High.Value - result.Offset.Value;
                                if (lag < 0) lag = 0;

                                Metrics.RecordConsumerLag(lag);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException)
                {
                    //TODO: log consumer exception
                    Metrics.RecordConsumerError();
                }
                catch (Exception)
                {
                    //TODO: log exception
                    Metrics.RecordConsumerError();
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }
}
