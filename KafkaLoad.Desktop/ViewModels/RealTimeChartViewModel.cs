using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.Services.Visualization;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace KafkaLoad.Desktop.ViewModels
{
    public class RealTimeChartViewModel : ReactiveObject, IDisposable
    {
        private readonly IMetricsService _metricsService;
        private IDisposable? _subscription;

        // Producer
        public MetricSeriesBuffer ProducerThroughput { get; }
        public MetricSeriesBuffer ProducerLatency { get; }
        public MetricSeriesBuffer ProducerMsgRate { get; }
        public MetricSeriesBuffer ProducerErrors { get; }

        // Consumer
        public MetricSeriesBuffer ConsumerThroughput { get; }
        public MetricSeriesBuffer ConsumerLatency { get; }
        public MetricSeriesBuffer ConsumerMsgRate { get; }
        public MetricSeriesBuffer ConsumerErrors { get; }

        private long _refreshCounter;
        public long RefreshCounter
        {
            get => _refreshCounter;
            set => this.RaiseAndSetIfChanged(ref _refreshCounter, value);
        }

        public RealTimeChartViewModel(IMetricsService metricsService)
        {
            _metricsService = metricsService;

            ProducerThroughput = new MetricSeriesBuffer("Producer MB/s");
            ProducerLatency = new MetricSeriesBuffer("Producer Latency (ms)");
            ConsumerThroughput = new MetricSeriesBuffer("Consumer MB/s");
            ConsumerLatency = new MetricSeriesBuffer("Consumer Latency (ms)");

            ProducerMsgRate = new MetricSeriesBuffer("Producer Msg/s");
            ProducerErrors = new MetricSeriesBuffer("Producer Errors");
            ConsumerMsgRate = new MetricSeriesBuffer("Consumer Msg/s");
            ConsumerErrors = new MetricSeriesBuffer("Consumer Errors");

            _subscription = _metricsService.MetricsStream
                .Sample(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnMetricsReceived);
        }

        private void OnMetricsReceived(GlobalMetricsSnapshot snapshot)
        {
            double time = snapshot.Duration.TotalSeconds;
            if (time > 300_000_000) return;

            // 1. Throughput MB/s
            ProducerThroughput.AddPoint(time, snapshot.Producer.ThroughputBytes / 1024.0 / 1024.0);
            ConsumerThroughput.AddPoint(time, snapshot.Consumer.ThroughputBytes / 1024.0 / 1024.0);

            // 2. Latency
            ProducerLatency.AddPoint(time, snapshot.Producer.AvgLatMs);
            ConsumerLatency.AddPoint(time, snapshot.Consumer.AvgLatencyMs);

            // 3. Throughput Msg/s (Нове)
            ProducerMsgRate.AddPoint(time, snapshot.Producer.ThroughputMsg);
            ConsumerMsgRate.AddPoint(time, snapshot.Consumer.ThroughputMsg);

            // 4. Errors
            ProducerErrors.AddPoint(time, snapshot.Producer.ErrorMsgsSent);
            ConsumerErrors.AddPoint(time, snapshot.Consumer.ErrorMsgsConsumed);

            RefreshCounter++;
        }

        public void Reset()
        {
            ProducerThroughput.Clear();
            ProducerLatency.Clear();
            ConsumerThroughput.Clear();
            ConsumerLatency.Clear();
            RefreshCounter = 0;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
