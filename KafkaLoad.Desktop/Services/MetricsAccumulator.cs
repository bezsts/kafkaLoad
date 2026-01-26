using System;
using System.Threading;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services;

public class MetricsAccumulator
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

    public LiveMetrics GetSnapshot()
    {
        long count = Interlocked.Read(ref _latencyCount);
        double avgLat = count > 0 ? (double)Interlocked.Read(ref _totalLatencySumMs) / count : 0;

        return new LiveMetrics(
            DateTime.Now,
            Interlocked.Read(ref _totalMsgsSent),
            Interlocked.Read(ref _totalBytesSent),
            Interlocked.Read(ref _successMsgsSent),
            Interlocked.Read(ref _errorMsgsSent),
            0,
            0,
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