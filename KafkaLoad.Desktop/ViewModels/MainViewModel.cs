using System;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class MainViewModel : ReactiveValidationObject
{
    public ProducerConfigViewModel ProducerConfigViewModel { get; }
    public ConsumerConfigViewModel ConsumerConfigViewModel { get; }
    public TestScenarioViewModel TestScenarioViewModel { get; }


    public MainViewModel(
        IConfigRepository<CustomProducerConfig> producerConfigRepository, 
        IConfigRepository<CustomConsumerConfig> consumerConfigRepository,
        IConfigRepository<TestScenario> testScenarioRepository, 
        IKafkaClientFactory kafkaClientFactory)
    {
        ProducerConfigViewModel = new ProducerConfigViewModel(producerConfigRepository);
        ConsumerConfigViewModel = new ConsumerConfigViewModel(consumerConfigRepository);
        TestScenarioViewModel = new TestScenarioViewModel(
            producerConfigRepository, 
            consumerConfigRepository, 
            testScenarioRepository);
    }
}
