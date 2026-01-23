using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigurationManager(), 
            typeof(IConfigurationManager));

        Locator.CurrentMutable.RegisterConstant(
            new KafkaClientFactory(), 
            typeof(IKafkaClientFactory));

        Locator.CurrentMutable.Register(() => 
            new ProducerConfigurationView(), typeof(IViewFor<ProducerConfigurationViewModel>));
        Locator.CurrentMutable.Register(() => 
            new ConsumerConfigurationView(), typeof(IViewFor<ConsumerConfigurationViewModel>));
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configManager = Locator.Current.GetService<IConfigurationManager>()!;
            var kafkaFactory = Locator.Current.GetService<IKafkaClientFactory>()!;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(configManager, kafkaFactory)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}