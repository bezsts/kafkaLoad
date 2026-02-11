using System;
using System.Collections.ObjectModel;
using System.Linq;
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

public class TestScenarioEditorViewModel : ReactiveValidationObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    private readonly IConfigRepository<CustomProducerConfig> _producerConfigRepository;
    private readonly IConfigRepository<CustomConsumerConfig> _consumerConfigRepository;
    private readonly IConfigRepository<TestScenario> _testScenarioRepository;
    
    private TestScenario _model;
    private readonly string _originalName;

    public IObservable<Unit> SaveComplete => SaveCommand;

    public TestScenarioEditorViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository,
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository,
        TestScenario? modelToEdit = null)
    {
        _producerConfigRepository = producerConfigRepository;
        _consumerConfigRepository = consumerConfigRepository;
        _testScenarioRepository = testScenarioRepository;
        
        if (modelToEdit != null)
        {
            _model = Clone(modelToEdit);
            _originalName = _model.Name;
        }
        else
        {
            _model = new TestScenario();
            _originalName = string.Empty;
        }

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
            viewModel => viewModel.ConsumerCount,
            consumerCount => consumerCount is not null,
            "Consumer count is required");

        this.ValidationRule(
            viewModel => viewModel.MessageSize,
            messageSize => messageSize is not null,
            "Message size is required");

        this.ValidationRule(
            viewModel => viewModel.Duration,
            duration => duration is not null,
            "Duration is required");
    }

    private TestScenario Clone(TestScenario source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<TestScenario>(json)!;
    }

    private async Task LoadConfigurationsAsync()
    {
        Producers.Clear();
        Consumers.Clear();

        var producerList = await _producerConfigRepository.GetAllAsync();
        foreach (var p in producerList) Producers.Add(p);

        var consumerList = await _consumerConfigRepository.GetAllAsync();
        foreach (var c in consumerList) Consumers.Add(c);

        if (_model.ProducerConfig != null)
        {
            var match = Enumerable.FirstOrDefault(Producers, p => p.Name == _model.ProducerConfig.Name);
            if (match != null) SelectedProducer = match;
        }

        if (_model.ConsumerConfig != null)
        {
            var match = Enumerable.FirstOrDefault(Consumers, c => c.Name == _model.ConsumerConfig.Name);
            if (match != null) SelectedConsumer = match;
        }
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
            if (_model.ProducerConfig != value)
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
            if (_model.ConsumerConfig != value)
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
            if (_model.ProducerCount != value)
            {
                _model.ProducerCount = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? ConsumerCount
    {
        get => _model.ConsumerCount;
        set
        {
            if (_model.ConsumerCount != value)
            {
                _model.ConsumerCount = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? MessageSize
    {
        get => _model.MessageSize;
        set
        {
            if (_model.MessageSize != value)
            {
                _model.MessageSize = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? Duration
    {
        get => _model.Duration;
        set
        {
            if (_model.Duration != value)
            {
                _model.Duration = value;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    private async Task SaveConfigAsync()
    {
        if (_model.Name != _originalName && await _testScenarioRepository.ExistsAsync(_model.Name))
        {
            throw new Exception($"Test Scenario with name '{_model.Name}' already exists!");
        }

        if (!string.IsNullOrEmpty(_originalName) && _model.Name != _originalName)
        {
            await _testScenarioRepository.DeleteAsync(_originalName);
        }

        await _testScenarioRepository.SaveAsync(_model);
    }
}
