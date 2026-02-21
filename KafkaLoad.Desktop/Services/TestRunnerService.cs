using Confluent.Kafka;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Generators;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.Services.Workers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services;

public class TestRunnerService : ITestRunnerService
{
    private readonly IKafkaClientFactory _clientFactory;
    private readonly IMetricsService _metricsService;
    private CancellationTokenSource? _cts;

    private readonly List<IDisposable> _activeClients = new();
    private readonly object _clientsLock = new();

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public TestRunnerService(IKafkaClientFactory clientFactory, IMetricsService metricsService)
    {
        _clientFactory = clientFactory;
        _metricsService = metricsService;
    }

    public async Task RunTestAsync(TestScenario scenario)
    {
        if (IsRunning) return;

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

            // --- 1. Start Producers ---
            if (scenario.ProducerConfig != null)
            {
                int pCount = scenario.ProducerCount ?? 1;
                for (int i = 0; i < pCount; i++)
                {
                    var nativeProducer = _clientFactory.CreateProducer(scenario.ProducerConfig, keySer, valSer);
                    var wrapper = new KafkaProducer<byte[], byte[]>(nativeProducer);
                    _activeClients.Add(wrapper);

                    lock (_clientsLock)
                    {
                        _activeClients.Add(wrapper);
                    }

                    var keyGen = DataGeneratorFactory.CreateKeyGenerator(scenario);
                    var valGen = DataGeneratorFactory.CreateValueGenerator(scenario);

                    var worker = new ProducerWorker(
                        wrapper,
                        _metricsService,
                        scenario.TopicName,
                        keyGen,
                        valGen,
                        throughputController
                    );

                    tasks.Add(Task.Run(() => worker.StartAsync(_cts.Token)));
                }
            }

            // --- 2. Start Consumers ---
            if (scenario.ConsumerConfig != null)
            {
                int cCount = scenario.ConsumerCount ?? 0;
                for (int i = 0; i < cCount; i++)
                {
                    var consumer = _clientFactory.CreateConsumer(scenario.ConsumerConfig, keyDeser, valDeser);

                    lock (_clientsLock)
                    {
                        _activeClients.Add(consumer);
                    }

                    var worker = new ConsumerWorker(
                        consumer,
                        _metricsService,
                        scenario.TopicName);

                    tasks.Add(Task.Run(() => worker.StartAsync(_cts.Token)));
                }
            }

            // --- 3. Wait for Test Duration ---
            try
            {
                int durationMs = (scenario.Duration ?? 60) * 1000;
                await Task.Delay(durationMs, _cts.Token);
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                _cts?.Cancel();

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                }

                _metricsService.Stop();
                CleanupClients();
                _cts = null;
            }
        });
    }

    public void StopTest()
    {
        _cts?.Cancel();
    }

    private void CleanupClients()
    {
        lock (_clientsLock)
        {
            foreach (var client in _activeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception e)
                {
                    //TODO: log errors
                    Debug.WriteLine($"Error disposing client: {e.Message}");
                }
            }
            _activeClients.Clear();
        }
    }
}
