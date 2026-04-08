using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.Infrastructure.Database;
using KafkaLoad.Infrastructure.Database.Repositories;
using KafkaLoad.Infrastructure.Kafka;
using KafkaLoad.UI.ViewModels;
using KafkaLoad.UI.ViewModels.Reports;
using KafkaLoad.UI.Views;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Serilog;
using Splat;
using System;
using System.IO;
using System.Reactive;

namespace KafkaLoad.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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

    private static KafkaLoadDbContext CreateDbContext()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KafkaLoad",
            "kafkaload.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var options = new DbContextOptionsBuilder<KafkaLoadDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var db = new KafkaLoadDbContext(options);
        db.Database.Migrate();
        return db;
    }

    private void RegisterAppServices()
    {
        var db = CreateDbContext();
        Locator.CurrentMutable.RegisterConstant(db, typeof(KafkaLoadDbContext));

        Locator.CurrentMutable.RegisterConstant(new MetricsService(), typeof(IMetricsService));
        Locator.CurrentMutable.RegisterConstant(new KafkaClientFactory(), typeof(IKafkaClientFactory));
        Locator.CurrentMutable.RegisterConstant(new KafkaTopicService(), typeof(IKafkaTopicService));

        var reportRepository = new SqliteTestReportRepository(db);
        _ = reportRepository.DeleteOldReportsAsync(olderThanDays: 90);
        Locator.CurrentMutable.RegisterConstant(reportRepository, typeof(ITestReportRepository));

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
        var db = Locator.Current.GetService<KafkaLoadDbContext>()!;

        Locator.CurrentMutable.RegisterConstant(
            new SqliteProducerConfigRepository(db),
            typeof(IConfigRepository<CustomProducerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new SqliteConsumerConfigRepository(db),
            typeof(IConfigRepository<CustomConsumerConfig>));

        Locator.CurrentMutable.RegisterConstant(
            new SqliteTestScenarioRepository(db),
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