using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services;

public class LoadEngineService
{
    public event EventHandler<RealTimeMetric>? OnMetricUpdated;
    public event EventHandler<TestResult>? OnTestFinished;

    private CancellationTokenSource? _cts;
    private long _messagesSentTotal = 0;
    private long _messagesSentInLastSecond = 0;

    private long _totalLatencyAccumulator = 0; 
    private long _latencySampleCount = 0;

    public async Task StartTestAsync(TestConfiguration config)
    {
        _cts = new CancellationTokenSource();
        
        // Скидаємо лічильники
        _messagesSentTotal = 0;
        _messagesSentInLastSecond = 0;
        _totalLatencyAccumulator = 0;
        _latencySampleCount = 0;

        // 1. Запускаємо таймер метрик (окремий потік, що цокає раз на секунду)
        _ = Task.Run(() => MetricsLoop(_cts.Token), _cts.Token);

        // 2. Створюємо конфіг Kafka Producer
        var producerConfig = new ProducerConfig
        {
            // Беремо дані з профілю
            BootstrapServers = config.ProducerSettings.BootstrapServers,
            LingerMs = config.ProducerSettings.LingerMs,
            BatchSize = config.ProducerSettings.BatchSize,
            Acks = config.ProducerSettings.Acks,
            CompressionType = config.ProducerSettings.Compression
        };

        // Підготовка даних (щоб не генерувати їх в циклі)
        byte[] payload = new byte[config.MessageSizeBytes];
        new Random().NextBytes(payload); 

        var tasks = new List<Task>();

        // 3. Запускаємо N продюсерів
        for (int i = 0; i < config.ProducerCount; i++)
        {
            tasks.Add(Task.Run(async () => 
            {
                using var producer = new ProducerBuilder<Null, byte[]>(producerConfig).Build();
                
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        
                        // Асинхронна відправка (Fire and forget для швидкості, або await для точності latency)
                        await producer.ProduceAsync(config.Topic, new Message<Null, byte[]> { Value = payload }, _cts.Token);
                        
                        sw.Stop();
                        
                        // Атомарні операції (безпечні для багатопоточності)
                        Interlocked.Increment(ref _messagesSentTotal);
                        Interlocked.Increment(ref _messagesSentInLastSecond);
                        Interlocked.Add(ref _totalLatencyAccumulator, sw.ElapsedMilliseconds);
                        Interlocked.Increment(ref _latencySampleCount);
                    }
                    catch (ProduceException<Null, byte[]> e)
                    {
                        Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                    }
                    catch (OperationCanceledException) { break; }
                }
            }, _cts.Token));
        }

        // 4. Чекаємо завершення часу тесту або скасування
        try 
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DurationSeconds), _cts.Token);
        }
        catch (TaskCanceledException) { /* Ігноруємо */ }
        finally
        {
            Stop();
        }

        await Task.WhenAll(tasks); // Чекаємо, поки всі потоки зупиняться

        // 5. Формуємо фінальний звіт
        var finalResult = new TestResult
        {
            RunDate = DateTime.Now,
            Configuration = config,
            TotalMessages = _messagesSentTotal,
            // Спрощений розрахунок середнього
            AvgThroughput = (double)_messagesSentTotal / config.DurationSeconds
        };

        OnTestFinished?.Invoke(this, finalResult);
    }

    private async Task MetricsLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1000, token); // Чекаємо 1 секунду

            // Читаємо скільки відправлено за цю секунду і обнуляємо лічильник
            long count = Interlocked.Exchange(ref _messagesSentInLastSecond, 0);
            
            // Рахуємо середню затримку
            long latSum = Interlocked.Exchange(ref _totalLatencyAccumulator, 0);
            long latCount = Interlocked.Exchange(ref _latencySampleCount, 0);
            double avgLat = latCount > 0 ? (double)latSum / latCount : 0;

            var metric = new RealTimeMetric(DateTime.Now, count, avgLat, Interlocked.Read(ref _messagesSentTotal));
            
            // Повідомляємо UI
            OnMetricUpdated?.Invoke(this, metric);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
