using System;
using System.Threading;

namespace KafkaLoad.Desktop.Services.Metrics.Accumulators
{
    public abstract class BaseMetricsAccumulator
    {
        protected long TotalMessages;
        protected long TotalBytes;
        protected long SuccessMessages;
        protected long ErrorMessages;
        protected long TotalLatencySumMs;
        protected long LatencyCount;

        protected double MaxLatencyMs;
        protected readonly object MaxLatLock = new object();

        protected readonly int[] LatencyBuckets = new int[10000];
        protected long OverflowLatencyCount;

        public void AddSuccess(int bytes, double latencyMs)
        {
            Interlocked.Increment(ref TotalMessages);
            Interlocked.Increment(ref SuccessMessages);
            Interlocked.Add(ref TotalBytes, bytes);

            // Track latency
            Interlocked.Add(ref TotalLatencySumMs, (long)latencyMs);
            Interlocked.Increment(ref LatencyCount);

            if (latencyMs > MaxLatencyMs)
            {
                lock (MaxLatLock)
                {
                    if (latencyMs > MaxLatencyMs)
                    {
                        MaxLatencyMs = latencyMs;
                    }
                }
            }

            int bucketIndex = (int)latencyMs;

            if (bucketIndex >= 0 && bucketIndex < LatencyBuckets.Length)
            {
                Interlocked.Increment(ref LatencyBuckets[bucketIndex]);
            }
            else if (bucketIndex >= LatencyBuckets.Length)
            {
                Interlocked.Increment(ref OverflowLatencyCount);
            }
        }

        public virtual void AddError()
        {
            Interlocked.Increment(ref TotalMessages);
            Interlocked.Increment(ref ErrorMessages);
        }

        public virtual void Reset()
        {
            Interlocked.Exchange(ref TotalMessages, 0);
            Interlocked.Exchange(ref TotalBytes, 0);
            Interlocked.Exchange(ref SuccessMessages, 0);
            Interlocked.Exchange(ref ErrorMessages, 0);
            Interlocked.Exchange(ref TotalLatencySumMs, 0);
            Interlocked.Exchange(ref LatencyCount, 0);

            lock (MaxLatLock)
            {
                MaxLatencyMs = 0;
            }

            Array.Clear(LatencyBuckets, 0, LatencyBuckets.Length);
            Interlocked.Exchange(ref OverflowLatencyCount, 0);
        }

        // --- Helper Calculation Methods ---

        protected double CalculateAvgLatency()
        {
            long count = Interlocked.Read(ref LatencyCount);
            long sum = Interlocked.Read(ref TotalLatencySumMs);
            return count > 0 ? (double)sum / count : 0;
        }

        protected double CalculateThroughputMsg(double elapsedSeconds)
        {
            return elapsedSeconds > 0 ? Interlocked.Read(ref TotalMessages) / elapsedSeconds : 0;
        }

        protected double CalculateThroughputBytes(double elapsedSeconds)
        {
            return elapsedSeconds > 0 ? Interlocked.Read(ref TotalBytes) / elapsedSeconds : 0;
        }

        protected double CalculateErrorRatePercent()
        {
            long total = Interlocked.Read(ref TotalMessages);
            long errors = Interlocked.Read(ref ErrorMessages);
            return total > 0 ? ((double)errors / total) * 100.0 : 0;
        }

        protected double CalculateP95Latency()
        {
            long totalRecorded = Interlocked.Read(ref LatencyCount);
            if (totalRecorded == 0) return 0;

            long targetCount = (long)(totalRecorded * 0.95);
            long accumulatedCount = 0;

            for (int i = 0; i < LatencyBuckets.Length; i++)
            {
                accumulatedCount += LatencyBuckets[i];

                if (accumulatedCount >= targetCount)
                {
                    return i;
                }
            }

            return LatencyBuckets.Length;
        }
    }
}