using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Metrics;
using KafkaLoad.Core.Models.Reports;
using KafkaLoad.Core.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.UI.ViewModels;

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

    private string _topicDisplayInfo = "Not checked";
    public string TopicDisplayInfo
    {
        get => _topicDisplayInfo;
        set => this.RaiseAndSetIfChanged(ref _topicDisplayInfo, value);
    }

    private string _bootstrapServers = string.Empty;
    public string BootstrapServers
    {
        get => _bootstrapServers;
        set => this.RaiseAndSetIfChanged(ref _bootstrapServers, value);
    }

    private string _topicName = string.Empty;
    public string TopicName
    {
        get => _topicName;
        set => this.RaiseAndSetIfChanged(ref _topicName, value);
    }

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
                scenario.IsConfigValid &&
                topicFound == true
        );

        StartTestCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            ChartViewModel.Reset();
            if (SelectedTestScenario == null) return;

            Log.Information("User clicked 'Start Test' for scenario: '{ScenarioName}'", SelectedTestScenario.Name);

            IsRunning = true;
            StatusText = "Initializing Workers...";
            try
            {
                Debug.WriteLine("Test Started");
                var request = new TestRunRequest
                {
                    Scenario = SelectedTestScenario,
                    BootstrapServers = BootstrapServers,
                    TopicName = TopicName
                };

                await _testRunner.RunTestAsync(request);

                var chartData = ChartViewModel?.GetTimeSeriesData() ?? new Dictionary<string, List<TimeSeriesPoint>>();

                await _testRunner.GenerateAndSaveReportAsync(request, chartData);

                StatusText = "Completed & Report Saved";
                Log.Information("Test scenario '{ScenarioName}' completed naturally and report was saved.", SelectedTestScenario.Name);
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                Log.Error(ex, "A fatal error occurred in the UI layer while running test scenario '{ScenarioName}'.", SelectedTestScenario.Name);
            }
            finally
            {
                IsRunning = false;
                if (!string.IsNullOrEmpty(_testRunner.ForceStopReason))
                {
                    StatusText = $"⚠ Force stopped: {_testRunner.ForceStopReason}";
                    Log.Warning("Test force stopped: {Reason}", _testRunner.ForceStopReason);
                }
                else
                {
                    StatusText = "Test Finished";
                }
            }
        }, canStart);

        StopTestCommand = ReactiveCommand.Create(() =>
        {
            Log.Information("User clicked 'Stop Test' button.");
            StatusText = "Stopping...";
            _testRunner.StopTest();
        },
        this.WhenAnyValue(x => x.IsRunning));

        RefreshTopicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            Log.Debug("User clicked 'Refresh Topic' button.");
            await CheckTopicAvailability();
        }, this.WhenAnyValue(x => x.BootstrapServers, x => x.TopicName,
            (servers, topic) => !string.IsNullOrWhiteSpace(servers) && !string.IsNullOrWhiteSpace(topic)));

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            _ = LoadConfigurationsAsync();

            _metricsService.MetricsStream
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(snapshot =>
                {
                    Metrics = snapshot;
                    if (IsRunning) StatusText = $"Running: {snapshot.Duration:mm\\:ss}";
                })
                .DisposeWith(disposables);
        });

        // Auto-check topic when both servers and topic name are filled in
        this.WhenAnyValue(x => x.BootstrapServers, x => x.TopicName)
            .Throttle(TimeSpan.FromMilliseconds(800))
            .Where(t => !string.IsNullOrWhiteSpace(t.Item1) && !string.IsNullOrWhiteSpace(t.Item2))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(async _ =>
            {
                try { await CheckTopicAvailability(); }
                catch (Exception ex) { Log.Error(ex, "Auto topic check failed."); }
            });
    }

    public Task RefreshScenariosAsync() => LoadConfigurationsAsync();

    private async Task LoadConfigurationsAsync()
    {
        TestScenarios.Clear();

        var testScenariosList = await _testScenarioRepository.GetAllAsync();
        foreach (var t in testScenariosList) TestScenarios.Add(t);
    }

    private async Task CheckTopicAvailability()
    {
        if (string.IsNullOrWhiteSpace(BootstrapServers) || string.IsNullOrWhiteSpace(TopicName))
        {
            IsTopicFound = null;
            TopicDisplayInfo = "Not checked";
            return;
        }

        IsTopicFound = null;
        TopicDisplayInfo = $"Checking '{TopicName}'...";

        try
        {
            bool exists = await _topicService.TopicExistsAsync(BootstrapServers, TopicName);

            if (exists)
            {
                IsTopicFound = true;
                TopicDisplayInfo = $"✓ {TopicName}";
                Log.Debug("Topic '{TopicName}' verified successfully on brokers.", TopicName);
            }
            else
            {
                IsTopicFound = false;
                TopicDisplayInfo = $"✗ '{TopicName}' not found";
                Log.Warning("Topic verification failed: '{TopicName}' not found or unreachable on '{Servers}'.", TopicName, BootstrapServers);
            }
        }
        catch (Exception ex)
        {
            IsTopicFound = false;
            TopicDisplayInfo = $"Error: {ex.Message}";
            Log.Error(ex, "Exception while checking topic '{TopicName}'.", TopicName);
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
                // Reset topic check when scenario changes
                IsTopicFound = null;
                TopicDisplayInfo = "Not checked";
            }
        }
    }

    // --- Commands ---
    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> StopTestCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshTopicCommand { get; }
}
