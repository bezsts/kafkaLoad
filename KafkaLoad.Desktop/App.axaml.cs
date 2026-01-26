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
        Locator.CurrentMutable.RegisterConstant(jsonConfigManager, typeof(IConfigRepository));

        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomProducerConfig>(jsonConfigManager, "Producers"),
            typeof(IConfigRepository<CustomProducerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomConsumerConfig>(jsonConfigManager, "Consumers"),
            typeof(IConfigRepository<CustomConsumerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<TestScenario>(jsonConfigManager, "TestScenarios"),
            typeof(IConfigRepository<TestScenario>));

        Locator.CurrentMutable.RegisterConstant(
            new KafkaClientFactory(), 
            typeof(IKafkaClientFactory));

        Locator.CurrentMutable.RegisterConstant(new MetricsService(), typeof(IMetricsService));

        Locator.CurrentMutable.RegisterLazySingleton(() => 
            new TestRunnerService(
                Locator.Current.GetService<IKafkaClientFactory>()!,
                Locator.Current.GetService<IMetricsService>()!
            ), 
            typeof(ITestRunnerService));

        Locator.CurrentMutable.Register(() => 
            new ProducerConfigView(), typeof(IViewFor<ProducerConfigViewModel>));
        Locator.CurrentMutable.Register(() => 
            new ConsumerConfigView(), typeof(IViewFor<ConsumerConfigViewModel>));
        Locator.CurrentMutable.Register(() => 
            new TestScenarioView(), typeof(IViewFor<TestScenarioViewModel>));
        Locator.CurrentMutable.Register(() => 
            new TestRunnerView(), typeof(IViewFor<TestRunnerViewModel>));
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var producerConfigRepository = Locator.Current.GetService<IConfigRepository<CustomProducerConfig>>()!;
            var consumerConfigRepository = Locator.Current.GetService<IConfigRepository<CustomConsumerConfig>>()!;
            var testScenarioRepository = Locator.Current.GetService<IConfigRepository<TestScenario>>()!;
            //var kafkaFactory = Locator.Current.GetService<IKafkaClientFactory>()!;
            var testRunnerService = Locator.Current.GetService<ITestRunnerService>()!;
            var metricsService = Locator.Current.GetService<IMetricsService>()!;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    producerConfigRepository, 
                    consumerConfigRepository,
                    testScenarioRepository, 
                    //kafkaFactory,
                    testRunnerService,
                    metricsService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}