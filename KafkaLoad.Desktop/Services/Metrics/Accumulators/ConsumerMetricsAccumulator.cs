using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Metrics.Accumulators;
using System.Threading;

namespace KafkaLoad.Desktop.Services;

public class ConsumerMetricsAccumulator : BaseMetricsAccumulator
{
    public ConsumerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        return new ConsumerMetricsSnapshot(
            Interlocked.Read(ref TotalMessages),
            Interlocked.Read(ref TotalBytes),
            Interlocked.Read(ref SuccessMessages),
            Interlocked.Read(ref ErrorMessages),
            CalculateThroughputMsg(elapsedSeconds),
            CalculateThroughputBytes(elapsedSeconds),
            CalculateAvgLatency()
        );
    }
}
