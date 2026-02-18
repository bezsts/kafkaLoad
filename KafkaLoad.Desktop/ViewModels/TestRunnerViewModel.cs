using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;

namespace KafkaLoad.Desktop.ViewModels;

public class TestRunnerViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly ITestRunnerService _testRunner;
    private readonly IMetricsService _metricsService;
    private readonly IConfigRepository<TestScenario> _testScenarioRepository;
    private readonly IKafkaTopicService _topicService;
    
    public RealTimeChartViewModel ChartViewModel { get; }

    public ViewModelActivator Activator { get; } = new();

    private bool? _isTopicFound;
    public bool? IsTopicFound
    {
        get => _isTopicFound;
        set => this.RaiseAndSetIfChanged(ref _isTopicFound, value);
    }

    private string _topicDisplayInfo = "None";
    public string TopicDisplayInfo
    {
        get => _topicDisplayInfo;
        set => this.RaiseAndSetIfChanged(ref _topicDisplayInfo, value);
    }

    private readonly ObservableAsPropertyHelper<bool> _canStart;
    public bool CanStart => _canStart.Value;

    public TestRunnerViewModel(
        ITestRunnerService testRunner, 
        IMetricsService metricsService,
        IConfigRepository<TestScenario> testScenarioRepository,
        IKafkaTopicService topicService)
    {
        _testRunner = testRunner;
        _metricsService = metricsService;
        _testScenarioRepository = testScenarioRepository;
        _topicService = topicService;

        ChartViewModel = new RealTimeChartViewModel(metricsService);

        var canStart = this.WhenAnyValue(
            x => x.IsRunning,
            x => x.SelectedTestScenario,
            x => x.IsTopicFound,
            (running, scenario, topicFound) =>
                !running &&             
                scenario != null &&     
                topicFound == true       
        );

        StartTestCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            ChartViewModel.Reset();
            if (SelectedTestScenario == null) return;
            IsRunning = true;
            StatusText = "Initializing Workers...";
            try
            {
                await _testRunner.RunTestAsync(SelectedTestScenario);
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
                StatusText = "Test Finished";
            }
        }, canStart);

        StopTestCommand = ReactiveCommand.Create(() =>
        {
            StatusText = "Stopping...";
            _testRunner.StopTest();
        }, 
        this.WhenAnyValue(x => x.IsRunning));

        RefreshTopicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedTestScenario != null)
            {
                await CheckTopicAvailability(SelectedTestScenario);
            }
        }, this.WhenAnyValue(x => x.SelectedTestScenario).Select(x => x != null));

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();

            _metricsService.MetricsStream
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(snapshot =>
                {
                    Metrics = snapshot;
                    if (IsRunning) StatusText = $"Running: {snapshot.Duration:mm\\:ss}";
                })
                .DisposeWith(disposables);
        });

        this.WhenAnyValue(x => x.SelectedTestScenario)
            .Where(scenario => scenario != null)
            .Subscribe(async scenario => await CheckTopicAvailability(scenario!));
    }

    private async Task LoadConfigurationsAsync()
    {
        TestScenarios.Clear();

        var testScenariosList = await _testScenarioRepository.GetAllAsync();
        foreach (var t in testScenariosList) TestScenarios.Add(t);
    }

    private async Task CheckTopicAvailability(TestScenario? scenario)
    {
        if (scenario == null)
        {
            IsTopicFound = null;
            TopicDisplayInfo = "None";
            return;
        }

        IsTopicFound = null;
        TopicDisplayInfo = $"Checking '{scenario.TopicName}'...";

        string? servers = scenario.ProducerConfig?.BootstrapServers ??
                          scenario.ConsumerConfig?.BootstrapServers;

        if (string.IsNullOrEmpty(servers))
        {
            IsTopicFound = false;
            TopicDisplayInfo = "Error: No Bootstrap Servers configured!";
            return;
        }

        bool exists = await _topicService.TopicExistsAsync(servers, scenario.TopicName);

        if (exists)
        {
            IsTopicFound = true;
            TopicDisplayInfo = scenario.TopicName;
        }
        else
        {
            IsTopicFound = false;
            TopicDisplayInfo = $"Error: '{scenario.TopicName}' not found!";
        }
    }

    // --- Properties for UI Binding ---

    private GlobalMetricsSnapshot? _metrics;
    public GlobalMetricsSnapshot? Metrics
    {
        get => _metrics;
        set => this.RaiseAndSetIfChanged(ref _metrics, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public ObservableCollection<TestScenario> TestScenarios { get; } = new();
    private TestScenario? _selectedTestScenario;
    public TestScenario? SelectedTestScenario
    {
        get => _selectedTestScenario;
        set
        {
            if (_selectedTestScenario != value)
            {
                _selectedTestScenario = value;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---
    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> StopTestCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshTopicCommand { get; }
}
