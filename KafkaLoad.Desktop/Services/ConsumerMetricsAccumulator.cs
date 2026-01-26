using System;
using System.Threading;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services;

public class ConsumerMetricsAccumulator
{
    private long _totalMsgsConsumed;
    private long _totalBytesConsumed;
    private long _successMsgsConsumed;
    private long _errorMsgsConsumed;
    private long _totalLatencySumMs;
    private long _latencyCount;

    public void AddSuccess(int bytes, double latencyMs)
    {
        Interlocked.Increment(ref _totalMsgsConsumed);
        Interlocked.Increment(ref _successMsgsConsumed);
        Interlocked.Add(ref _totalBytesConsumed, bytes);
        Interlocked.Add(ref _totalLatencySumMs, (long)latencyMs);
        Interlocked.Increment(ref _latencyCount);
    }

    public void AddError()
    {
        Interlocked.Increment(ref _totalMsgsConsumed);
        Interlocked.Increment(ref _errorMsgsConsumed);
    }

    public ConsumerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        long count = Interlocked.Read(ref _latencyCount);
        double avgLat = count > 0 ? (double)Interlocked.Read(ref _totalLatencySumMs) / count : 0;

        double throughputMsg = elapsedSeconds > 0 ? Interlocked.Read(ref _totalMsgsConsumed) / elapsedSeconds : 0;
        double throughputBytes = elapsedSeconds > 0 ? Interlocked.Read(ref _totalBytesConsumed) / elapsedSeconds : 0;

        return new ConsumerMetricsSnapshot(
            Interlocked.Read(ref _totalMsgsConsumed),
            Interlocked.Read(ref _totalBytesConsumed),
            Interlocked.Read(ref _successMsgsConsumed),
            Interlocked.Read(ref _errorMsgsConsumed),
            throughputMsg,
            throughputBytes,
            avgLat
        );
    }

    public void Reset()
    {
        _totalMsgsConsumed = 0;
        _totalBytesConsumed = 0;
        _successMsgsConsumed = 0;
        _errorMsgsConsumed = 0;
        _totalLatencySumMs = 0;
        _latencyCount = 0;
    }
}
