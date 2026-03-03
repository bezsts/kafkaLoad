using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.Services.Reports.Interfaces;
using KafkaLoad.Desktop.ViewModels.Reports;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

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
