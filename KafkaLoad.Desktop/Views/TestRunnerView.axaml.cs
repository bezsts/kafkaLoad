using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Diagnostics;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace KafkaLoad.Desktop.Views;

public partial class TestRunnerView : ReactiveUserControl<TestRunnerViewModel>
{
    private AvaPlot? _producerChart;
    private AvaPlot? _consumerChart;

    public TestRunnerView()
    {
        InitializeComponent();

        _producerChart = this.FindControl<AvaPlot>("ProducerChart");
        _consumerChart = this.FindControl<AvaPlot>("ConsumerChart");

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm != null)
                .Subscribe(vm =>
                {
                    if (vm.ChartViewModel == null)
                    {
                        return;
                    }

                    SetupProducerChart();
                    SetupConsumerChart();

                    vm.ChartViewModel
                        .WhenAnyValue(x => x.RefreshCounter)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => UpdateCharts())
                        .DisposeWith(disposables);
                })
                .DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupProducerChart()
    {
        if (_producerChart == null) return;

        _producerChart.Plot.Clear();

        _producerChart.Plot.FigureBackground.Color = Color.FromHex("#1e1e1e");
        _producerChart.Plot.DataBackground.Color = Color.FromHex("#252526");
        _producerChart.Plot.Axes.Color(Color.FromHex("#d4d4d4"));
        _producerChart.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

        _producerChart.Plot.Title("Throughput History", size: 14);
        _producerChart.Plot.Axes.Left.Label.Text = "MB/s";
    }

    private void SetupConsumerChart()
    {
        if (_consumerChart == null) return;

        _consumerChart.Plot.Clear();

        _consumerChart.Plot.FigureBackground.Color = Color.FromHex("#1e1e1e");
        _consumerChart.Plot.DataBackground.Color = Color.FromHex("#252526");
        _consumerChart.Plot.Axes.Color(Color.FromHex("#d4d4d4"));
        _consumerChart.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

        _consumerChart.Plot.Title("Throughput History", size: 14);
        _consumerChart.Plot.Axes.Left.Label.Text = "MB/s";
    }

    private void UpdateCharts()
    {
        Debug.WriteLine("[VIEW] UpdateCharts called");

        if (ViewModel?.ChartViewModel == null) return;
        var charts = ViewModel.ChartViewModel;

        if (_producerChart != null)
        {
            _producerChart.Plot.PlottableList.Clear();

            var prodScatter = _producerChart.Plot.Add.Scatter(
                charts.ProducerThroughput.XValues.ToArray(),
                charts.ProducerThroughput.YValues.ToArray()
            );

            prodScatter.Color = Color.FromHex("#3b82f6"); // Blue
            prodScatter.LineWidth = 2;
            prodScatter.MarkerSize = 0;

            _producerChart.Plot.Axes.AutoScale();
            _producerChart.Refresh();
        }

        if (_consumerChart != null)
        {
            _consumerChart.Plot.PlottableList.Clear();

            var consScatter = _consumerChart.Plot.Add.Scatter(
                charts.ConsumerThroughput.XValues.ToArray(),
                charts.ConsumerThroughput.YValues.ToArray()
            );

            consScatter.Color = Color.FromHex("#10B981"); // Green
            consScatter.LineWidth = 2;
            consScatter.MarkerSize = 0;

            _consumerChart.Plot.Axes.AutoScale();
            _consumerChart.Refresh();
        }
    }
}