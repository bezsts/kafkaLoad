using KafkaLoad.Core.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Engine.Workers
{
    public abstract class BaseWorker
    {
        protected readonly IMetricsService Metrics;
        protected readonly string Topic;

        protected BaseWorker(IMetricsService metrics, string topic)
        {
            Metrics = metrics;
            Topic = topic;
        }

        public abstract Task StartAsync(CancellationToken ct);
    }
}
