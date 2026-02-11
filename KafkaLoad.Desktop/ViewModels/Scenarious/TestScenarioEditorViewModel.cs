using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
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

    public TestScenarioEditorViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository,
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository,
        TestScenario? modelToEdit = null) : base(testScenarioRepository, modelToEdit)
    {
        _producerConfigRepository = producerConfigRepository;
        _consumerConfigRepository = consumerConfigRepository;

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();
        });
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(vm => vm.Name, n => !string.IsNullOrWhiteSpace(n), "Name is required");
        this.ValidationRule(vm => vm.TopicName, t => !string.IsNullOrWhiteSpace(t), "Topic is required");
        this.ValidationRule(vm => vm.SelectedProducer, p => p is not null, "Producer is required");
        this.ValidationRule(vm => vm.SelectedConsumer, c => c is not null, "Consumer is required");
        this.ValidationRule(vm => vm.ProducerCount, c => c > 0, "Producer count > 0");
        this.ValidationRule(vm => vm.MessageSize, s => s > 0, "Message size > 0");
        this.ValidationRule(vm => vm.Duration, d => d > 0, "Duration > 0");
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
}
