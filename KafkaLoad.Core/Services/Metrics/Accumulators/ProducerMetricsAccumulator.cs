using KafkaLoad.Core.Models.Metrics;
using KafkaLoad.Core.Services.Metrics.Accumulators;
using System.Threading;

namespace KafkaLoad.Core.Services;

public class ProducerMetricsAccumulator : BaseMetricsAccumulator
{
    private long _recordQueueTimeSumMs;
    private long _recordQueueTimeCount;
    public ProducerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        long queueTimeCount = Interlocked.Read(ref _recordQueueTimeCount);
        long queueTimeSum = Interlocked.Read(ref _recordQueueTimeSumMs);

        return new ProducerMetricsSnapshot(
            TotalMessagesAttempted: Interlocked.Read(ref TotalMessages),
            SuccessMessagesSent: Interlocked.Read(ref SuccessMessages),
            ErrorMessages: Interlocked.Read(ref ErrorMessages),
            InFlightMessages: Interlocked.Read(ref InFlightMessages),
            TotalBytesSent: Interlocked.Read(ref TotalBytes),

            ErrorRatePercent: CalculateErrorRatePercent(),

            ThroughputMsgSec: CalculateThroughputMsg(elapsedSeconds),
            ThroughputBytesSec: CalculateThroughputBytes(elapsedSeconds),

            MaxLatencyMs: MaxLatencyMs,

            P50Lat: CalculateP50Latency(),
            P95Lat: CalculateP95Latency(),
            P99Lat: CalculateP99Latency()
        );
    }

    public void AddQueueTime(double queueTimeMs)
    {
        Interlocked.Add(ref _recordQueueTimeSumMs, (long)queueTimeMs);
        Interlocked.Increment(ref _recordQueueTimeCount);
    }

    public override void Reset()
    {
        base.Reset();
        Interlocked.Exchange(ref _recordQueueTimeSumMs, 0);
        Interlocked.Exchange(ref _recordQueueTimeCount, 0);
    }
}