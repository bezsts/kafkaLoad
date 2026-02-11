using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class TestRunnerService : ITestRunnerService
{
    private readonly IKafkaClientFactory _clientFactory;
    private readonly IMetricsService _metricsService;
    private CancellationTokenSource? _cts;
    //private readonly List<IKafkaProducer<byte[], byte[]>> _activeProducers = new();
    //private readonly List<IConsumer<byte[], byte[]>> _activeConsumers = new();

    private readonly List<IDisposable> _activeClients = new();

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

        CleanupClients();

        var tasks = new List<Task>();

        //TODO: implement generic serializers (json, string)
        var keySer = Serializers.ByteArray;
        var valSer = Serializers.ByteArray;
        var keyDeser = Deserializers.ByteArray;
        var valDeser = Deserializers.ByteArray;

        // --- 1. Start Producers ---
        if (scenario.ProducerConfig != null)
        {
            int pCount = scenario.ProducerCount ?? 1;
            for (int i = 0; i < pCount; i++)
            {
                var nativeProducer = _clientFactory.CreateProducer(scenario.ProducerConfig, keySer, valSer);
                var wrapper = new KafkaProducer<byte[], byte[]>(nativeProducer);
                _activeClients.Add(wrapper);

                var worker = new ProducerWorker(
                    wrapper,
                    _metricsService,
                    scenario.TopicName,
                    scenario.MessageSize ?? 1024);

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
                _activeClients.Add(consumer);

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
            StopTest();
        }
    }

    public void StopTest()
    {
        _metricsService.Stop();

        _cts?.Cancel();
        CleanupClients();
        _cts = null;
    }

    private void CleanupClients()
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
                Console.WriteLine($"Error disposing client: {e.Message}");
            }
        }
        _activeClients.Clear();
    }
}
