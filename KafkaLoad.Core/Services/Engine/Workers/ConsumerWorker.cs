using Confluent.Kafka;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Engine.Workers;

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
        var testStartUtc = DateTime.UtcNow;

        try
        {
            Log.Information("ConsumerWorker starting. Subscribing to topic: {Topic}", Topic);
            _consumer.Subscribe(Topic);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Consume blocks until a message is available or the token is cancelled
                    var result = _consumer.Consume(ct);

                    if (result != null && !result.IsPartitionEOF)
                    {
                        // Skip messages produced before this test started (stale messages from previous runs)
                        if (result.Message.Timestamp.UtcDateTime < testStartUtc) continue;

                        // Calculate End-to-End latency
                        double latencyMs = (DateTime.UtcNow - result.Message.Timestamp.UtcDateTime).TotalMilliseconds;

                        // Sanity check for clock skew
                        if (latencyMs < 0) latencyMs = 0;

                        int bytes = (result.Message.Key?.Length ?? 0) + (result.Message.Value?.Length ?? 0);
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
                    Log.Information("ConsumerWorker cancelled cleanly on topic: {Topic}", Topic);
                    break;
                }
                catch (ConsumeException ex)
                {
                    Log.Error(ex, "Kafka ConsumeException on topic {Topic}. Reason: {Reason}", Topic, ex.Error.Reason);
                    Metrics.RecordConsumerError();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error in ConsumerWorker loop on topic {Topic}", Topic);
                    Metrics.RecordConsumerError();
                }
            }
        }
        finally
        {
            Log.Information("ConsumerWorker shutting down on topic: {Topic}", Topic);
            _consumer.Close();
        }
    }
}
