using System;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class MainViewModel : ReactiveValidationObject
{
    public ProducerConfigViewModel ProducerConfigViewModel { get; }
    public ConsumerConfigViewModel ConsumerConfigViewModel { get; }


    public MainViewModel(
        IConfigManager configManager, 
        IKafkaClientFactory kafkaClientFactory)
    {
        ProducerConfigViewModel = new ProducerConfigViewModel(configManager);
        ConsumerConfigViewModel = new ConsumerConfigViewModel(configManager);
    }
}
