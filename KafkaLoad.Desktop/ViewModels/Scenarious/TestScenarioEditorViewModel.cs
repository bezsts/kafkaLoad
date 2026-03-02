using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.ViewModels;

public class TestScenarioEditorViewModel : BaseConfigViewModel<TestScenario>, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new ViewModelActivator();

    private readonly IConfigRepository<CustomProducerConfig> _producerConfigRepository;
    private readonly IConfigRepository<CustomConsumerConfig> _consumerConfigRepository;

    private bool _isFixedContentVisible;
    public bool IsFixedContentVisible
    {
        get => _isFixedContentVisible;
        set => this.RaiseAndSetIfChanged(ref _isFixedContentVisible, value);
    }

    private bool _isMessageSizeVisible;
    public bool IsMessageSizeVisible
    {
        get => _isMessageSizeVisible;
        set => this.RaiseAndSetIfChanged(ref _isMessageSizeVisible, value);
    }

    private bool _isTargetThroughputVisible = true;
    public bool IsTargetThroughputVisible
    {
        get => _isTargetThroughputVisible;
        set => this.RaiseAndSetIfChanged(ref _isTargetThroughputVisible, value);
    }

    private bool _isSpikeVisible;
    public bool IsSpikeVisible
    {
        get => _isSpikeVisible;
        set => this.RaiseAndSetIfChanged(ref _isSpikeVisible, value);
    }

    public TestScenarioEditorViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository,
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository,
        TestScenario? modelToEdit = null) : base(testScenarioRepository, modelToEdit)
    {
        _producerConfigRepository = producerConfigRepository;
        _consumerConfigRepository = consumerConfigRepository;

        this.WhenAnyValue(x => x.ValueStrategy)
            .Subscribe(strategy =>
            {
                IsFixedContentVisible = strategy == ValueGenerationStrategy.Fixed;
                IsMessageSizeVisible = strategy != ValueGenerationStrategy.Fixed;
            });

        this.WhenAnyValue(x => x.TestType)
            .Subscribe(type =>
            {
                IsTargetThroughputVisible = type != TestType.Spike;
                IsSpikeVisible = type == TestType.Spike;
            });

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();
        });
    }

    protected override void InitializeValidation()
    {
        // ==========================================
        // 1. BASIC FIELD REQUIREMENTS
        // ==========================================
        this.ValidationRule(vm => vm.Name, n => !string.IsNullOrWhiteSpace(n), "Name is required");
        this.ValidationRule(vm => vm.TopicName, t => !string.IsNullOrWhiteSpace(t), "Topic is required");
        this.ValidationRule(vm => vm.SelectedProducer, p => p is not null, "Producer is required");
        this.ValidationRule(vm => vm.SelectedConsumer, c => c is not null, "Consumer is required");
        this.ValidationRule(vm => vm.ProducerCount, c => c > 0, "Producer count must be > 0");

        // ==========================================
        // 2. DATA GENERATION STRATEGY
        // ==========================================
        var fixedTemplateValid = this.WhenAnyValue(
            x => x.FixedTemplate,
            x => x.ValueStrategy,
            (template, strategy) => strategy != ValueGenerationStrategy.Fixed || !string.IsNullOrWhiteSpace(template)
        );
        this.ValidationRule(vm => vm.FixedTemplate, fixedTemplateValid, "Message content is required for Fixed strategy");

        var messageSizeValid = this.WhenAnyValue(
            x => x.MessageSize,
            x => x.ValueStrategy,
            (size, strategy) => strategy == ValueGenerationStrategy.Fixed || (size > 0)
        );
        this.ValidationRule(vm => vm.MessageSize, messageSizeValid, "Message size must be > 0");

        // ==========================================
        // 3. TEST DURATION LOGIC
        // ==========================================
        var durationValid = this.WhenAnyValue(
            x => x.TestType,
            x => x.Duration,
            (type, duration) =>
            {
                if (!duration.HasValue) return false;

                return type switch
                {
                    TestType.Soak => duration.Value >= 60,   // Minimum 1 minute for Soak
                    TestType.Stress => duration.Value >= 30, // Minimum 30s to draw 4 steps properly
                    _ => duration.Value >= 10                // Minimum 10s for Load/Spike to ramp up
                };
            }
        );
        this.ValidationRule(vm => vm.Duration, durationValid,
            "Invalid duration: >= 60s for Soak, >= 30s for Stress, >= 10s for others");

        // ==========================================
        // 4. THROUGHPUT LOGIC
        // ==========================================
        var targetValid = this.WhenAnyValue(
            x => x.TestType,
            x => x.TargetThroughput,
            (type, target) => type == TestType.Spike || (target.HasValue && target.Value > 0)
        );
        this.ValidationRule(vm => vm.TargetThroughput, targetValid, "Target Throughput must be > 0");

        var baseValid = this.WhenAnyValue(
            x => x.TestType,
            x => x.BaseThroughput,
            (type, baseTp) => type != TestType.Spike || (baseTp.HasValue && baseTp.Value > 0)
        );
        this.ValidationRule(vm => vm.BaseThroughput, baseValid, "Base Throughput must be > 0");

        var spikeValid = this.WhenAnyValue(
            x => x.TestType,
            x => x.BaseThroughput,
            x => x.SpikeThroughput,
            (type, baseTp, spikeTp) =>
            {
                if (type != TestType.Spike) return true;
                if (!spikeTp.HasValue || !baseTp.HasValue) return false;

                return spikeTp.Value > baseTp.Value;
            }
        );
        this.ValidationRule(vm => vm.SpikeThroughput, spikeValid, "Spike rate must be strictly greater than Base rate");
    }

    private async Task LoadConfigurationsAsync()
    {
        Producers.Clear();
        Consumers.Clear();

        var producerList = await _producerConfigRepository.GetAllAsync();
        foreach (var p in producerList) Producers.Add(p);

        var consumerList = await _consumerConfigRepository.GetAllAsync();
        foreach (var c in consumerList) Consumers.Add(c);

        if (Model.ProducerConfig != null)
            SelectedProducer = Producers.FirstOrDefault(p => p.Name == Model.ProducerConfig.Name);

        if (Model.ConsumerConfig != null)
            SelectedConsumer = Consumers.FirstOrDefault(c => c.Name == Model.ConsumerConfig.Name);
    }

    // --- Properties Wrappers ---

    public string Name
    {
        get => Model.Name;
        set => SetProperty(value, Model.Name, v => Model.Name = v);
    }
    public string TopicName
    {
        get => Model.TopicName;
        set => SetProperty(value, Model.TopicName, v => Model.TopicName = v);
    }

    public ObservableCollection<CustomProducerConfig> Producers { get; } = new();
    public CustomProducerConfig? SelectedProducer
    {
        get => Model.ProducerConfig;
        set => SetProperty(value, Model.ProducerConfig, v => Model.ProducerConfig = v);
    }

    public ObservableCollection<CustomConsumerConfig> Consumers { get; } = new();
    public CustomConsumerConfig? SelectedConsumer
    {
        get => Model.ConsumerConfig;
        set => SetProperty(value, Model.ConsumerConfig, v => Model.ConsumerConfig = v);
    }

    public int? ProducerCount
    {
        get => Model.ProducerCount;
        set => SetProperty(value, Model.ProducerCount, v => Model.ProducerCount = v);
    }

    public int? ConsumerCount
    {
        get => Model.ConsumerCount;
        set => SetProperty(value, Model.ConsumerCount, v => Model.ConsumerCount = v);
    }

    public List<KeyGenerationStrategy> KeyStrategyOptions { get; } = Enum.GetValues<KeyGenerationStrategy>().ToList();
    public KeyGenerationStrategy KeyStrategy
    {
        get => Model.KeyStrategy;
        set => SetProperty(value, Model.KeyStrategy, v => Model.KeyStrategy = v);
    }

    public List<ValueGenerationStrategy> ValueStrategyOptions { get; } = Enum.GetValues<ValueGenerationStrategy>().ToList();
    public ValueGenerationStrategy ValueStrategy
    {
        get => Model.ValueStrategy;
        set => SetProperty(value, Model.ValueStrategy, v => Model.ValueStrategy = v);
    }

    public string? FixedTemplate
    {
        get => Model.FixedTemplate;
        set => SetProperty(value, Model.FixedTemplate, v => Model.FixedTemplate = v);
    }

    public int? MessageSize
    {
        get => Model.MessageSize;
        set => SetProperty(value, Model.MessageSize, v => Model.MessageSize = v);
    }

    public int? Duration
    {
        get => Model.Duration;
        set => SetProperty(value, Model.Duration, v => Model.Duration = v);
    }

    public List<TestType> TestTypeOptions { get; } = Enum.GetValues<TestType>().ToList();

    public TestType TestType
    {
        get => Model.TestType;
        set => SetProperty(value, Model.TestType, v => Model.TestType = v);
    }

    public int? TargetThroughput
    {
        get => Model.TargetThroughput;
        set => SetProperty(value, Model.TargetThroughput, v => Model.TargetThroughput = v);
    }

    public int? BaseThroughput
    {
        get => Model.BaseThroughput;
        set => SetProperty(value, Model.BaseThroughput, v => Model.BaseThroughput = v);
    }

    public int? SpikeThroughput
    {
        get => Model.SpikeThroughput;
        set => SetProperty(value, Model.SpikeThroughput, v => Model.SpikeThroughput = v);
    }
}
