using System;
using System.Collections.ObjectModel;
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

    public ViewModelActivator Activator { get; } = new();

    public TestRunnerViewModel(
        ITestRunnerService testRunner, 
        IMetricsService metricsService,
        IConfigRepository<TestScenario> testScenarioRepository)
    {
        _testRunner = testRunner;
        _metricsService = metricsService;
        _testScenarioRepository = testScenarioRepository;

        var canStart = this.WhenAnyValue(
            x => x.IsRunning, 
            x => x.SelectedTestScenario,
            (running, scenario) => !running && scenario != null
        );

        StartTestCommand = ReactiveCommand.CreateFromTask(async () =>
        {
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

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();

            _metricsService.MetricsStream
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(snapshot =>
                {
                    Metrics = snapshot;
                    
                    if (IsRunning)
                    {
                        StatusText = $"Running: {snapshot.Duration:mm\\:ss}";
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private async Task LoadConfigurationsAsync()
    {
        TestScenarios.Clear();

        var testScenariosList = await _testScenarioRepository.GetAllAsync();
        foreach (var t in testScenariosList) TestScenarios.Add(t);
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
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> StartTestCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> StopTestCommand { get; }
}
