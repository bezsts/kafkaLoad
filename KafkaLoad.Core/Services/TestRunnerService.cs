using Confluent.Kafka;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Reports;
using KafkaLoad.Core.Services.Generators;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.Core.Services.Engine.Workers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KafkaLoad.Core.Services.Engine;

namespace KafkaLoad.Core.Services;

public class TestRunnerService : ITestRunnerService
{
    private readonly IKafkaClientFactory _clientFactory;
    private readonly IMetricsService _metricsService;
    private CancellationTokenSource? _cts;
    private readonly ITestReportRepository _reportRepository;

    private readonly List<IDisposable> _activeClients = new();
    private readonly object _clientsLock = new();

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public TestRunnerService(
        IKafkaClientFactory clientFactory, 
        IMetricsService metricsService,
        ITestReportRepository reportRepository)
    {
        _clientFactory = clientFactory;
        _metricsService = metricsService;
        _reportRepository = reportRepository;
    }

    public async Task RunTestAsync(TestRunRequest request)
    {
        var scenario = request.Scenario;

        if (IsRunning)
        {
            Log.Warning("Attempted to start a test while another test is already running.");
            return;
        }

        Log.Information("Starting Test Scenario: '{ScenarioName}'. Type: {TestType}, Topic: {Topic}, Duration: {Duration}s",
            scenario.Name, scenario.TestType, request.TopicName, scenario.Duration);

        _cts = new CancellationTokenSource();
        _metricsService.Reset();

        await Task.Run(async () =>
        {
            CleanupClients();

            var tasks = new List<Task>();

            var keySer = Serializers.ByteArray;
            var valSer = Serializers.ByteArray;
            var keyDeser = Deserializers.ByteArray;
            var valDeser = Deserializers.ByteArray;

            var throughputController = new ThroughputController(scenario);
            var replenisherTask = Task.Run(() => throughputController.RunReplenisherAsync(_cts.Token));
            tasks.Add(replenisherTask);

            // --- Start Producers ---
            if (scenario.ProducerConfig != null)
            {
                int pCount = scenario.ProducerCount ?? 1;
                Log.Information("Initializing {Count} Producer(s)...", pCount);

                for (int i = 0; i < pCount; i++)
                {
                    var producer = _clientFactory.CreateProducer(scenario.ProducerConfig, request.BootstrapServers, keySer, valSer);

                    lock (_clientsLock)
                    {
                        _activeClients.Add(producer);
                    }

                    var keyGen = DataGeneratorFactory.CreateKeyGenerator(scenario);
                    var valGen = DataGeneratorFactory.CreateValueGenerator(scenario);

                    var worker = new ProducerWorker(
                        producer,
                        _metricsService,
                        request.TopicName,
                        keyGen,
                        valGen,
                        throughputController
                    );

                    tasks.Add(Task.Run(() => worker.StartAsync(_cts.Token)));
                }
            }

            // --- Start Consumers ---
            if (scenario.ConsumerConfig != null)
            {
                int cCount = scenario.ConsumerCount ?? 0;

                if (cCount > 0)
                {
                    Log.Information("Initializing {Count} Consumer(s)...", cCount);
                }

                for (int i = 0; i < cCount; i++)
                {
                    var consumer = _clientFactory.CreateConsumer(scenario.ConsumerConfig, request.BootstrapServers, keyDeser, valDeser);

                    lock (_clientsLock)
                    {
                        _activeClients.Add(consumer);
                    }

                    var worker = new ConsumerWorker(
                        consumer,
                        _metricsService,
                        request.TopicName);

                    tasks.Add(Task.Run(() => worker.StartAsync(_cts.Token)));
                }
            }

            Log.Information("All workers initialized. Test is now running...");

            // --- Wait for Test Duration ---
            try
            {
                int durationMs = (scenario.Duration ?? 60) * 1000;
                await Task.Delay(durationMs, _cts.Token);
                Log.Information("Test duration completed naturally.");
            }
            catch (TaskCanceledException)
            {
                Log.Information("Test was manually stopped by the user.");
            }
            finally
            {
                _cts?.Cancel();

                try
                {
                    Log.Debug("Waiting for all worker tasks to finish cleanly...");
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "One or more worker tasks threw an exception during shutdown.");
                }

                throughputController.Dispose();

                _metricsService.Stop();

                CleanupClients();
                _cts = null;

                Log.Information("Test Scenario '{ScenarioName}' fully stopped and resources released.", scenario.Name);
            }
        });
    }

    public async Task GenerateAndSaveReportAsync(TestRunRequest request, Dictionary<string, List<TimeSeriesPoint>> timeSeriesData)
    {
        var scenario = request.Scenario;
        Log.Information("Generating final report for Scenario: '{ScenarioName}'", scenario.Name);

        var snapshot = _metricsService.CurrentSnapshot;
        if (snapshot == null)
        {
            Log.Warning("Cannot generate report: Metrics snapshot is null.");
            return;
        }

        var report = new TestReport
        {
            ScenarioName = scenario.Name ?? "Unnamed Scenario",
            TestType = scenario.TestType,
            DurationSeconds = scenario.Duration ?? 0,

            ConfigSnapshot = new TestScenarioConfigSnapshot
            {
                TopicName = request.TopicName,
                ProducersCount = scenario.ProducerCount ?? 1,
                ConsumersCount = scenario.ConsumerCount ?? 0,
                MessageSize = scenario.MessageSize ?? 0,
                TargetThroughput = scenario.TargetThroughput
            },

            ProducerMetrics = new ProducerReportMetrics
            {
                TotalMessagesAttempted = snapshot.Producer.TotalMessagesAttempted,
                SuccessMessagesSent = snapshot.Producer.SuccessMessagesSent,
                ErrorMessages = snapshot.Producer.ErrorMessages,
                TotalBytesSent = snapshot.Producer.TotalBytesSent,
                ErrorRatePercent = snapshot.Producer.ErrorRatePercent,
                ThroughputMsgSec = snapshot.Producer.ThroughputMsgSec,
                ThroughputBytesSec = snapshot.Producer.ThroughputBytesSec,
                AvgLatencyMs = snapshot.Producer.AvgLatencyMs,
                MaxLatencyMs = snapshot.Producer.MaxLatencyMs,
                P95Lat = snapshot.Producer.P95Lat
            },

            TimeSeriesData = timeSeriesData
        };

        if (scenario.ConsumerCount > 0 && snapshot.Consumer != null)
        {
            report.ConsumerMetrics = new ConsumerReportMetrics
            {
                TotalMessagesConsumed = snapshot.Consumer.TotalMessagesConsumed,
                TotalBytesConsumed = snapshot.Consumer.TotalBytesConsumed,
                SuccessMessagesConsumed = snapshot.Consumer.SuccessMessagesConsumed,
                ErrorMessagesConsumed = snapshot.Consumer.ErrorMessagesConsumed,
                ThroughputMsgSec = snapshot.Consumer.ThroughputMsgSec,
                ThroughputBytesSec = snapshot.Consumer.ThroughputBytesSec,
                AvgEndToEndLatencyMs = snapshot.Consumer.AvgEndToEndLatencyMs,
                MaxEndToEndLatencyMs = snapshot.Consumer.MaxEndToEndLatencyMs,
                MaxConsumerLag = snapshot.Consumer.MaxConsumerLag,
                FinalConsumerLag = snapshot.Consumer.FinalConsumerLag
            };
        }

        if (_reportRepository != null)
        {
            await _reportRepository.SaveReportAsync(report);
            Log.Debug("Report successfully generated and passed to repository.");
        }
    }

    public void StopTest()
    {
        Log.Information("StopTest() called. Requesting cancellation...");
        _cts?.Cancel();
    }

    private void CleanupClients()
    {
        lock (_clientsLock)
        {
            int count = _activeClients.Count;
            if (count == 0) return;

            Log.Debug("Cleaning up {Count} active Kafka clients...", count);

            foreach (var client in _activeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error disposing Kafka client during cleanup.");
                }
            }
            _activeClients.Clear();

            Log.Debug("Cleanup complete.");
        }
    }
}
