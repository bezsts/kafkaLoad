using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using KafkaLoad.Desktop.Services;
using KafkaLoad.Desktop.Models;
using System.Collections.ObjectModel;

namespace KafkaLoad.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly LoadEngineService _engine = new LoadEngineService();

    public MainWindowViewModel()
    {
        // Коли двигун каже "ось нові метрики", ми оновлюємо текст
        _engine.OnMetricUpdated += (sender, metric) =>
        {
            // Важливо: оновлення UI має бути в головному потоці
            Dispatcher.UIThread.Invoke(() =>
            {
                Logs += $"[{metric.Timestamp:HH:mm:ss}] RPS: {metric.ThroughputRps:F0} | Latency: {metric.AverageLatencyMs:F1} ms\n";
            });
        };

        // Коли тест закінчився сам (по часу)
        _engine.OnTestFinished += (sender, result) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Logs += $"[System]: Test finished. Total: {result.TotalMessages}\n";
                IsRunning = false;
            });
        };
    }
    // --- Списки Профілів ---
    [ObservableProperty] private ObservableCollection<ProducerProfile> _producerProfiles;
    [ObservableProperty] private ObservableCollection<ConsumerProfile> _consumerProfiles;

    // --- Обрані Профілі (для запуску тесту) ---
    [ObservableProperty] private ProducerProfile _selectedProducerProfile;
    [ObservableProperty] private ConsumerProfile _selectedConsumerProfile;

    // --- Профіль, який ми редагуємо зараз (для вкладки налаштувань) ---
    [ObservableProperty] private ProducerProfile _editingProducerProfile;

    // --- Вхідні параметри (Input) ---
    [ObservableProperty]
    private string _bootstrapServers = "localhost:9092";

    [ObservableProperty]
    private string _topicName = "test-topic";

    [ObservableProperty]
    private int _producerCount = 1;

    [ObservableProperty]
    private int _messageSize = 1024;

    [ObservableProperty]
    private int _durationSeconds = 10;

    // --- Стан інтерфейсу ---

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTestCommand))]
    private bool _isRunning = false;

    // Текстове поле для логів/метрик
    [ObservableProperty]
    private string _logs = "Ready to start...\n";

    // --- Команди ---
    [RelayCommand]
    private void AddNewProducerProfile()
    {
        var newProfile = new ProducerProfile { Name = "New Producer Config" };
        ProducerProfiles.Add(newProfile);
        SelectedProducerProfile = newProfile; // Одразу обираємо його
        EditingProducerProfile = newProfile;  // І відкриваємо для редагування
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartTest()
    {
        IsRunning = true;
        Logs = $"[System]: Connecting to {BootstrapServers}...\n";

        // Збираємо конфігурацію з полів вводу
        var config = new TestConfiguration
        {
            BootstrapServers = BootstrapServers,
            Topic = TopicName,
            ProducerCount = ProducerCount,
            MessageSizeBytes = MessageSize,
            DurationSeconds = DurationSeconds
        };

        // Запускаємо реальний тест (без await, бо він працює у фоні, а ми чекаємо подій)
        // Але оскільки наш метод StartTestAsync в Engine чекає завершення - можна і await
        try 
        {
            await _engine.StartTestAsync(config);
        }
        catch (Exception ex)
        {
            Logs += $"[Error]: {ex.Message}\n";
            IsRunning = false;
        }
    }

    private bool CanStart() => !IsRunning;
}