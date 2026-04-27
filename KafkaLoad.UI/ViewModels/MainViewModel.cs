using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.UI.ViewModels.Reports;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.UI.ViewModels;

public class MainViewModel : ReactiveValidationObject
{
    public ProducerConfigViewModel ProducerConfigViewModel { get; }
    public ConsumerConfigViewModel ConsumerConfigViewModel { get; }
    public ClientsConfigViewModel ClientsConfigViewModel { get; }
    public TestScenariosViewModel TestScenariosViewModel { get; }
    public TestRunnerViewModel TestRunnerViewModel { get; }
    public ReportsViewModel ReportsViewModel { get; }

    private readonly ObservableAsPropertyHelper<bool> _isTestRunning;
    public bool IsTestRunning => _isTestRunning.Value;

    public MainViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository,
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository,
        //IKafkaClientFactory kafkaClientFactory,
        ITestRunnerService testRunnerService,
        IMetricsService metricsService,
        IKafkaTopicService kafkaTopicService,
        ITestReportRepository testReportRepository)
    {
        ProducerConfigViewModel = new ProducerConfigViewModel(producerConfigRepository);
        ConsumerConfigViewModel = new ConsumerConfigViewModel(consumerConfigRepository);

        ClientsConfigViewModel = new ClientsConfigViewModel(
            producerConfigRepository,
            consumerConfigRepository,
            testScenarioRepository
        );

        TestScenariosViewModel = new TestScenariosViewModel(
            testScenarioRepository,
            producerConfigRepository,
            consumerConfigRepository);
        TestRunnerViewModel = new TestRunnerViewModel(testRunnerService, metricsService, testScenarioRepository, kafkaTopicService);

        _isTestRunning = TestRunnerViewModel
            .WhenAnyValue(x => x.IsRunning)
            .ToProperty(this, x => x.IsTestRunning);

        ReportsViewModel = new ReportsViewModel(testReportRepository);

        ClientsConfigViewModel.ConfigDeleted += () =>
        {
            _ = TestScenariosViewModel.RefreshAsync();
            _ = TestRunnerViewModel.RefreshScenariosAsync();
        };
    }
}
