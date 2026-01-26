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
    private readonly List<IProducer<byte[], byte[]>> _activeProducers = new();
    private readonly List<IConsumer<byte[], byte[]>> _activeConsumers = new();

    
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
                var producer = _clientFactory.CreateProducer(scenario.ProducerConfig, keySer, valSer);
                _activeProducers.Add(producer);

                var worker = new ProducerWorker(
                    producer,
                    _metricsService,
                    scenario.TopicName,
                    scenario.MessageSize ?? 1024);

                tasks.Add(worker.StartAsync(_cts.Token));
            }
        }

        // --- 2. Start Consumers ---
        if (scenario.ConsumerConfig != null)
        {
            int cCount = scenario.ConsumerCount ?? 0;
            for (int i = 0; i < cCount; i++)
            {
                var consumer = _clientFactory.CreateConsumer(scenario.ConsumerConfig, keyDeser, valDeser);
                _activeConsumers.Add(consumer);

                var worker = new ConsumerWorker(
                    consumer,
                    _metricsService,
                    scenario.TopicName);

                tasks.Add(worker.StartAsync(_cts.Token));
            }
        }

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
        _cts?.Cancel();
        
        CleanupClients();
        
        _cts = null;
    }

    private void CleanupClients()
    {
        foreach (var p in _activeProducers)
        {
            try 
            {
                p.Flush(TimeSpan.FromSeconds(3)); 
                p.Dispose();
            }
            catch (Exception e) { Console.WriteLine($"Error disposing producer: {e.Message}"); }
        }
        _activeProducers.Clear();

        foreach (var c in _activeConsumers)
        {
            try 
            {
                c.Dispose();
            }
            catch (Exception e) { Console.WriteLine($"Error disposing consumer: {e.Message}"); }
        }
        _activeConsumers.Clear();
    }
}
