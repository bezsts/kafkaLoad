using System;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class MainViewModel : ReactiveValidationObject
{
    public ProducerConfigurationViewModel ProducerConfigurationViewModel { get; }
    public ConsumerConfigurationViewModel ConsumerConfigurationViewModel { get; }


    public MainViewModel(
        IConfigurationManager configurationManager, 
        IKafkaClientFactory kafkaClientFactory)
    {
        ProducerConfigurationViewModel = new ProducerConfigurationViewModel(configurationManager);
        ConsumerConfigurationViewModel = new ConsumerConfigurationViewModel(configurationManager);
    }
}
