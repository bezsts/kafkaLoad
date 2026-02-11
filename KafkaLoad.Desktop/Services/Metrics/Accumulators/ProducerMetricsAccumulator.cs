using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Metrics.Accumulators;
using System.Threading;

namespace KafkaLoad.Desktop.Services;

public class ProducerMetricsAccumulator : BaseMetricsAccumulator
{
    // TODO: Implement P95 calculation logic
    public ProducerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        return new ProducerMetricsSnapshot(
            Interlocked.Read(ref TotalMessages),
            Interlocked.Read(ref TotalBytes),
            Interlocked.Read(ref SuccessMessages),
            Interlocked.Read(ref ErrorMessages),
            CalculateThroughputMsg(elapsedSeconds),
            CalculateThroughputBytes(elapsedSeconds),
            CalculateAvgLatency(),
            0 //HACK: P95 placeholder
        );
    }

    public override void Reset()
    {
        base.Reset();
    }
}