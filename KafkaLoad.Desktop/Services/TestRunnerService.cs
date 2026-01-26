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
        _activeProducers.Clear();

        var tasks = new List<Task>();

        //TODO: implement generic serializers (json, string)
        var keySer = Serializers.ByteArray;
        var valSer = Serializers.ByteArray;

        int pCount = scenario.ProducerCount ?? 1;

        for (int i = 0; i < pCount; i++)
        {
            var producer = _clientFactory.CreateProducer(scenario.ProducerConfig!, keySer, valSer);
            _activeProducers.Add(producer);

            var worker = new ProducerWorker(
                producer, 
                _metricsService, 
                scenario.TopicName, 
                scenario.MessageSize ?? 1024);

            tasks.Add(worker.StartAsync(_cts.Token));
        }

        //TODO: add consumer workers

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
        
        foreach (var p in _activeProducers)
        {
            p.Flush(TimeSpan.FromSeconds(5));
            p.Dispose();
        }
        _activeProducers.Clear();
        _cts = null;
    }
}
