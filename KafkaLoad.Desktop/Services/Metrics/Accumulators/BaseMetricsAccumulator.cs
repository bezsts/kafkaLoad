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

        public void AddSuccess(int bytes, double latencyMs)
        {
            Interlocked.Increment(ref TotalMessages);
            Interlocked.Increment(ref SuccessMessages);
            Interlocked.Add(ref TotalBytes, bytes);

            // Track latency
            Interlocked.Add(ref TotalLatencySumMs, (long)latencyMs);
            Interlocked.Increment(ref LatencyCount);
        }

        public void AddError()
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
    }
}