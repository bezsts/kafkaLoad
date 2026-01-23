using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.ViewModels;
using KafkaLoad.Desktop.Views;
using ReactiveUI;
using Splat;

namespace KafkaLoad.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var jsonConfigManager = new JsonConfigManager();
        Locator.CurrentMutable.RegisterConstant(jsonConfigManager, typeof(IConfigManager));

        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomProducerConfig>(jsonConfigManager, "Producers"),
            typeof(IConfigRepository<CustomProducerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomConsumerConfig>(jsonConfigManager, "Consumers"),
            typeof(IConfigRepository<CustomConsumerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new KafkaClientFactory(), 
            typeof(IKafkaClientFactory));

        Locator.CurrentMutable.Register(() => 
            new ProducerConfigView(), typeof(IViewFor<ProducerConfigViewModel>));
        Locator.CurrentMutable.Register(() => 
            new ConsumerConfigView(), typeof(IViewFor<ConsumerConfigViewModel>));
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configManager = Locator.Current.GetService<IConfigManager>()!;
            var kafkaFactory = Locator.Current.GetService<IKafkaClientFactory>()!;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(configManager, kafkaFactory)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}