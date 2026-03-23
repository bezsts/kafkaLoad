using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.UI.ViewModels.Reports;
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
            consumerConfigRepository
        );

        TestScenariosViewModel = new TestScenariosViewModel(
            testScenarioRepository,
            producerConfigRepository,
            consumerConfigRepository);
        TestRunnerViewModel = new TestRunnerViewModel(testRunnerService, metricsService, testScenarioRepository, kafkaTopicService);

        ReportsViewModel = new ReportsViewModel(testReportRepository);
    }
}
