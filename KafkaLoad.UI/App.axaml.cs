using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.Infrastructure.Kafka;
using KafkaLoad.UI.ViewModels;
using KafkaLoad.UI.ViewModels.Reports;
using KafkaLoad.UI.Views;
using ReactiveUI;
using Serilog;
using Splat;
using System;
using System.Reactive;
using KafkaLoad.Infrastructure.Configuration.Json;
using KafkaLoad.Infrastructure.Reports;

namespace KafkaLoad.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        //RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        //{
        //    Log.Error(ex, "An unhandled exception occurred in a UI command.");

        //    // TODO: Here you could trigger a global Event/Message to show a nice Error Dialog to the user
        //});

        RegisterDependencies();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Orchestrates the registration of all application components.
    /// </summary>
    private void RegisterDependencies()
    {
        RegisterAppServices();

        RegisterRepositories();

        RegisterViews();
    }

    private void RegisterAppServices()
    {
        // Core services
        Locator.CurrentMutable.RegisterConstant(new JsonConfigManager(), typeof(IFileManager));
        Locator.CurrentMutable.RegisterConstant(new MetricsService(), typeof(IMetricsService));
        Locator.CurrentMutable.RegisterConstant(new KafkaClientFactory(), typeof(IKafkaClientFactory));
        Locator.CurrentMutable.RegisterConstant(new KafkaTopicService(), typeof(IKafkaTopicService));
        Locator.CurrentMutable.RegisterConstant(new JsonTestReportRepository(), typeof(ITestReportRepository));

        Locator.CurrentMutable.RegisterLazySingleton(() =>
            new TestRunnerService(
                Locator.Current.GetService<IKafkaClientFactory>()!,
                Locator.Current.GetService<IMetricsService>()!,
                Locator.Current.GetService<ITestReportRepository>()!
            ),
            typeof(ITestRunnerService));
    }

    private void RegisterRepositories()
    {
        var fileManager = Locator.Current.GetService<IFileManager>();

        // Producers
        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomProducerConfig>(fileManager!, "Producers"),
            typeof(IConfigRepository<CustomProducerConfig>));

        // Consumers
        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<CustomConsumerConfig>(fileManager!, "Consumers"),
            typeof(IConfigRepository<CustomConsumerConfig>));

        // Scenarios
        Locator.CurrentMutable.RegisterConstant(
            new JsonConfigRepository<TestScenario>(fileManager!, "TestScenarios"),
            typeof(IConfigRepository<TestScenario>));
    }

    private void RegisterViews()
    {
        Locator.CurrentMutable.Register(() => new ProducerConfigView(), typeof(IViewFor<ProducerConfigViewModel>));
        Locator.CurrentMutable.Register(() => new ConsumerConfigView(), typeof(IViewFor<ConsumerConfigViewModel>));
        Locator.CurrentMutable.Register(() => new ClientsConfigView(), typeof(IViewFor<ClientsConfigViewModel>));

        Locator.CurrentMutable.Register(() => new TestScenarioEditorView(), typeof(IViewFor<TestScenarioEditorViewModel>));
        Locator.CurrentMutable.Register(() => new TestScenariosView(), typeof(IViewFor<TestScenariosViewModel>));

        Locator.CurrentMutable.Register(() => new TestRunnerView(), typeof(IViewFor<TestRunnerViewModel>));

        Locator.CurrentMutable.Register(() => new ReportsView(), typeof(IViewFor<ReportsViewModel>));
    }

    /// <summary>
    /// Resolves dependencies and creates the Main Window.
    /// </summary>
    private MainWindow CreateMainWindow()
    {
        var resolver = Locator.Current;

        return new MainWindow
        {
            DataContext = new MainViewModel(
                resolver.GetService<IConfigRepository<CustomProducerConfig>>()!,
                resolver.GetService<IConfigRepository<CustomConsumerConfig>>()!,
                resolver.GetService<IConfigRepository<TestScenario>>()!,
                resolver.GetService<ITestRunnerService>()!,
                resolver.GetService<IMetricsService>()!,
                resolver.GetService<IKafkaTopicService>()!,
                resolver.GetService<ITestReportRepository>()!
            )
        };
    }
}