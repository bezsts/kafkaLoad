using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Confluent.Kafka;
using KafkaLoad.Desktop.Services;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly LoadEngineService _engine = new LoadEngineService();

    public MainWindowViewModel()
    {
        // --- Ініціалізація дефолтних даних ---
        ProducerProfiles = new ObservableCollection<ProducerProfile>
        {
            new ProducerProfile { Name = "Localhost (Fast)", BootstrapServers = "localhost:9092", Acks = Acks.Leader },
            new ProducerProfile { Name = "Production (Reliable)", BootstrapServers = "192.168.1.10:9092", Acks = Acks.All }
        };

        ConsumerProfiles = new ObservableCollection<ConsumerProfile>
        {
            new ConsumerProfile { Name = "Local Reader", GroupId = "test-group-1" }
        };

        // Обираємо перші елементи за замовчуванням
        SelectedProducerProfile = ProducerProfiles.First();
        SelectedConsumerProfile = ConsumerProfiles.First();

        // Підписка на події двигуна
        _engine.OnMetricUpdated += (s, m) => Dispatcher.UIThread.Invoke(() => 
            Logs += $"[{m.Timestamp:HH:mm:ss}] RPS: {m.ThroughputRps:F0} | Latency: {m.AverageLatencyMs:F1} ms\n");
            
        _engine.OnTestFinished += (s, r) => Dispatcher.UIThread.Invoke(() => 
            { Logs += $"Finished. Total: {r.TotalMessages}\n"; IsRunning = false; });
    }

    // --- Списки Профілів ---
    [ObservableProperty] 
    private ObservableCollection<ProducerProfile> _producerProfiles = new();

    [ObservableProperty] 
    private ObservableCollection<ConsumerProfile> _consumerProfiles = new();

    // --- Обрані Профілі (для запуску тесту) ---
    // ВИПРАВЛЕННЯ WARNING: Додано " = null!;", щоб заспокоїти компілятор
    [ObservableProperty] 
    private ProducerProfile _selectedProducerProfile = null!;

    [ObservableProperty] 
    private ConsumerProfile _selectedConsumerProfile = null!;

    // --- Профіль, який ми редагуємо зараз ---
    [ObservableProperty] 
    private ProducerProfile? _editingProducerProfile;


    // --- Загальні налаштування сценарію ---
    // ВИПРАВЛЕННЯ ERROR: Додані відсутні поля, які шукав XAML
    [ObservableProperty] private string _topicName = "test-topic";
    [ObservableProperty] private int _threads = 1;       // Це виправить помилку 'Threads'
    [ObservableProperty] private int _duration = 10;     // Це виправить помилку 'Duration'
    [ObservableProperty] private int _messageSize = 1024;
    
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _logs = "";

    // --- Команди ---

    [RelayCommand]
    private void AddNewProducerProfile()
    {
        var newProfile = new ProducerProfile { Name = "New Producer Config" };
        ProducerProfiles.Add(newProfile);
        SelectedProducerProfile = newProfile;
        EditingProducerProfile = newProfile;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartTest()
    {
        IsRunning = true;
        Logs += $"Starting test using profile: {SelectedProducerProfile.Name}...\n";

        var config = new TestConfiguration
        {
            ProducerSettings = SelectedProducerProfile,
            Topic = TopicName,
            ProducerCount = Threads,
            MessageSizeBytes = MessageSize,
            DurationSeconds = Duration
        };

        try
        {
            await _engine.StartTestAsync(config);
        }
        catch (Exception ex)
        {
            Logs += $"Error: {ex.Message}\n";
            IsRunning = false;
        }
    }

    private bool CanStart() => !IsRunning;
}