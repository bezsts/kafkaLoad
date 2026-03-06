using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Metrics.Accumulators;
using System.Threading;

namespace KafkaLoad.Desktop.Services;

public class ConsumerMetricsAccumulator : BaseMetricsAccumulator
{
    private long _maxConsumerLag;
    private long _finalConsumerLag;
    private readonly object _lagLock = new object();
    public ConsumerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        return new ConsumerMetricsSnapshot(
             TotalMessagesConsumed: Interlocked.Read(ref TotalMessages),
             TotalBytesConsumed: Interlocked.Read(ref TotalBytes),
             SuccessMessagesConsumed: Interlocked.Read(ref SuccessMessages),
             ErrorMessagesConsumed: Interlocked.Read(ref ErrorMessages),

             ThroughputMsgSec: CalculateThroughputMsg(elapsedSeconds),
             ThroughputBytesSec: CalculateThroughputBytes(elapsedSeconds),

             AvgEndToEndLatencyMs: CalculateAvgLatency(),
             MaxEndToEndLatencyMs: MaxLatencyMs,

             MaxConsumerLag: Interlocked.Read(ref _maxConsumerLag),
             FinalConsumerLag: Interlocked.Read(ref _finalConsumerLag)
         );
    }

    public void UpdateLag(long currentLag)
    {
        Interlocked.Exchange(ref _finalConsumerLag, currentLag);

        if (currentLag > _maxConsumerLag)
        {
            lock (_lagLock)
            {
                if (currentLag > _maxConsumerLag)
                {
                    _maxConsumerLag = currentLag;
                }
            }
        }
    }

    public override void Reset()
    {
        base.Reset();
        Interlocked.Exchange(ref _finalConsumerLag, 0);
        lock (_lagLock)
        {
            _maxConsumerLag = 0;
        }
    }
}
