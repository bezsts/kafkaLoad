using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class TestScenarioViewModel : ReactiveValidationObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    private readonly IConfigRepository<CustomProducerConfig> _producerConfigRepository;
    private readonly IConfigRepository<CustomConsumerConfig> _consumerConfigRepository;
    private readonly IConfigRepository<TestScenario> _testScenarioRepository;
    private TestScenario _model;

    public TestScenarioViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository,
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository)
    {
        _producerConfigRepository = producerConfigRepository;
        _consumerConfigRepository = consumerConfigRepository;
        _testScenarioRepository = testScenarioRepository;
        _model = new TestScenario();

        InitializeValidation();

        SaveCommand = ReactiveCommand.CreateFromTask(
            SaveConfigAsync,
            canExecute: this.IsValid(), 
            outputScheduler: RxApp.MainThreadScheduler
        );

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();
        });
    }

    protected void InitializeValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name is required");

        this.ValidationRule(
            viewModel => viewModel.SelectedProducer,
            producer => producer is not null,
            "Producer is required");

        this.ValidationRule(
            viewModel => viewModel.SelectedConsumer,
            consumer => consumer is not null,
            "Consumer is required");

        this.ValidationRule(
            viewModel => viewModel.TopicName,
            topic => !string.IsNullOrWhiteSpace(topic),
            "Topic name is required");

        this.ValidationRule(
            viewModel => viewModel.ProducerCount,
            producerCount => producerCount is not null,
            "Producer count is required");

        this.ValidationRule(
            viewModel => viewModel.MessageSize,
            messageSize => messageSize is not null,
            "Message size is required");

        this.ValidationRule(
            viewModel => viewModel.Duration,
            duration => duration is not null,
            "Duration is required");
    }

    private async Task LoadConfigurationsAsync()
    {
        Producers.Clear();
        Consumers.Clear();

        var producerList = await _producerConfigRepository.GetAllAsync();
        foreach (var p in producerList) Producers.Add(p);

        var consumerList = await _consumerConfigRepository.GetAllAsync();
        foreach (var c in consumerList) Consumers.Add(c);
    }

    // --- Properties Wrappers ---

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public ObservableCollection<CustomProducerConfig> Producers { get; } = new();
    public CustomProducerConfig? SelectedProducer
    {
        get => _model.ProducerConfig;
        set
        {
            if (_model.ProducerConfig != value && value is not null)
            {
                _model.ProducerConfig = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public ObservableCollection<CustomConsumerConfig> Consumers { get; } = new();
    public CustomConsumerConfig? SelectedConsumer
    {
        get => _model.ConsumerConfig;
        set
        {
            if (_model.ConsumerConfig != value && value is not null)
            {
                _model.ConsumerConfig = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public string TopicName
    {
        get => _model.TopicName;
        set
        {
            if (_model.TopicName != value)
            {
                _model.TopicName = value;
                this.RaisePropertyChanged();
            }
        }
    }
    public int? ProducerCount
    {
        get => _model.ProducerCount;
        set
        {
            if (_model.ProducerCount != value && value is not null)
            {
                _model.ProducerCount = (int)value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? ConsumerCount
    {
        get => _model.ConsumerCount;
        set
        {
            if (_model.ConsumerCount != value && value is not null)
            {
                _model.ConsumerCount = (int)value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? MessageSize
    {
        get => _model.MessageSize;
        set
        {
            if (_model.MessageSize != value && value is not null)
            {
                _model.MessageSize = (int)value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? Duration
    {
        get => _model.Duration;
        set
        {
            if (_model.Duration != value && value is not null)
            {
                _model.Duration = (int)value;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    private async Task SaveConfigAsync()
    {
        await _testScenarioRepository.SaveAsync(_model);
    }
}
