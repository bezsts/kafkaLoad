using System;
using System.Threading;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services;

public class ProducerMetricsAccumulator
{
    private long _totalMsgsSent;
    private long _totalBytesSent;
    private long _successMsgsSent;
    private long _errorMsgsSent;
    private long _totalLatencySumMs;
    private long _latencyCount;
    private double _p95Lat;

    public void AddSuccess(int bytes, double latencyMs)
    {
        Interlocked.Increment(ref _totalMsgsSent);
        Interlocked.Increment(ref _successMsgsSent);
        Interlocked.Add(ref _totalBytesSent, bytes);

        Interlocked.Add(ref _totalLatencySumMs, (long)latencyMs); 
        Interlocked.Increment(ref _latencyCount);
    }
    
    public void AddError()
    {
        Interlocked.Increment(ref _totalMsgsSent);
        Interlocked.Increment(ref _errorMsgsSent);
    }

    public ProducerMetricsSnapshot GetSnapshot(double elapsedSeconds)
    {
        long count = Interlocked.Read(ref _latencyCount);
        double avgLat = count > 0 ? (double)Interlocked.Read(ref _totalLatencySumMs) / count : 0;

        double throughputMsg = elapsedSeconds > 0 ? Interlocked.Read(ref _totalMsgsSent) / elapsedSeconds : 0;
        double throughputBytes = elapsedSeconds > 0 ? Interlocked.Read(ref _totalBytesSent) / elapsedSeconds : 0;

        return new ProducerMetricsSnapshot(
            Interlocked.Read(ref _totalMsgsSent),
            Interlocked.Read(ref _totalBytesSent),
            Interlocked.Read(ref _successMsgsSent),
            Interlocked.Read(ref _errorMsgsSent),
            throughputMsg,
            throughputBytes,
            avgLat,
            0 // TODO: implement p95
        );
    }

    public void Reset()
    {
        _totalMsgsSent = 0;
        _totalBytesSent = 0;
        _successMsgsSent = 0;
        _errorMsgsSent = 0;
        _totalLatencySumMs = 0;
        _latencyCount = 0;
        _p95Lat = 0;
    }
}