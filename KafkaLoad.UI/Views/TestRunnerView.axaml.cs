using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.UI.Helpers;
using KafkaLoad.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace KafkaLoad.UI.Views;

public partial class TestRunnerView : ReactiveUserControl<TestRunnerViewModel>
{
    private AvaPlot? _producerChart;
    private AvaPlot? _consumerChart;

    private ComboBox? _prodSelector;
    private ComboBox? _consSelector;

    public TestRunnerView()
    {
        InitializeComponent();

        _producerChart = this.FindControl<AvaPlot>("ProducerChart");
        _consumerChart = this.FindControl<AvaPlot>("ConsumerChart");

        _prodSelector = this.FindControl<ComboBox>("ProducerMetricSelector");
        _consSelector = this.FindControl<ComboBox>("ConsumerMetricSelector");

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm != null)
                .Subscribe(vm =>
                {
                    if (vm!.ChartViewModel == null)
                    {
                        return;
                    }

                    SetupProducerChart();
                    SetupConsumerChart();


                    vm.ChartViewModel
                        .WhenAnyValue(x => x.RefreshCounter)
                        .ObserveOn(AvaloniaScheduler.Instance)
                        .Subscribe(_ => UpdateCharts())
                        .DisposeWith(disposables);
                })
                .DisposeWith(disposables);

            if (_prodSelector != null)
            {
                _prodSelector.SelectionChanged += (s, e) => UpdateCharts();
            }

            if (_consSelector != null)
            {
                _consSelector.SelectionChanged += (s, e) => UpdateCharts();
            }
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

        _producerChart.Plot.FigureBackground.Color = ChartTheme.Background;
        _producerChart.Plot.DataBackground.Color = ChartTheme.Background;
        _producerChart.Plot.Axes.Color(ChartTheme.Text);
        _producerChart.Plot.Grid.MajorLineColor = ChartTheme.GridLines;

        _producerChart.Plot.Title("Throughput History", size: 14);
        _producerChart.Plot.Axes.Left.Label.Text = "MB/s";
    }

    private void SetupConsumerChart()
    {
        if (_consumerChart == null) return;

        _consumerChart.Plot.Clear();

        _consumerChart.Plot.FigureBackground.Color = ChartTheme.Background;
        _consumerChart.Plot.DataBackground.Color = ChartTheme.Background;
        _consumerChart.Plot.Axes.Color(ChartTheme.Text);
        _consumerChart.Plot.Grid.MajorLineColor = ChartTheme.GridLines;

        _consumerChart.Plot.Title("Throughput History", size: 14);
        _consumerChart.Plot.Axes.Left.Label.Text = "MB/s";
    }

    private void UpdateCharts()
    {
        if (ViewModel?.ChartViewModel == null) return;
        var charts = ViewModel.ChartViewModel;

        UpdateProducerChart(charts);
        UpdateConsumerChart(charts);
    }

    private void UpdateProducerChart(RealTimeChartViewModel charts)
    {
        if (_producerChart == null || _prodSelector == null) return;

        int index = _prodSelector.SelectedIndex;

        _producerChart.Plot.PlottableList.Clear();

        if (index == 2)
        {
            // Latency: draw P50, P95, P99 as three separate colored lines
            void AddLatencyLine(MetricSeriesBuffer buffer, ScottPlot.Color color, string legend)
            {
                var scatter = _producerChart.Plot.Add.Scatter(
                    buffer.XValues.ToArray(),
                    buffer.YValues.ToArray()
                );
                scatter.Color = color;
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
                scatter.LegendText = legend;
            }

            AddLatencyLine(charts.ProducerLatencyP50, ChartTheme.LatencyP50, "P50");
            AddLatencyLine(charts.ProducerLatencyP95, ChartTheme.LatencyP95, "P95");
            AddLatencyLine(charts.ProducerLatencyP99, ChartTheme.LatencyP99, "P99");

            _producerChart.Plot.ShowLegend(ScottPlot.Alignment.UpperLeft);
            _producerChart.Plot.Axes.Left.Label.Text = "ms";
            _producerChart.Plot.Title("Producer Latency (ms)", size: 12);
        }
        else
        {
            var buffer = index switch
            {
                1 => charts.ProducerMsgRate,
                3 => charts.ProducerErrors,
                _ => charts.ProducerThroughput
            };

            var (color, label) = ChartTheme.GetMetricStyle(index);

            var scatter = _producerChart.Plot.Add.Scatter(
                buffer.XValues.ToArray(),
                buffer.YValues.ToArray()
            );
            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;

            _producerChart.Plot.HideLegend();
            _producerChart.Plot.Axes.Left.Label.Text = label;
            _producerChart.Plot.Title($"{buffer.Title}", size: 12);
        }

        _producerChart.Plot.Axes.AutoScale();
        _producerChart.Refresh();
    }

    private void UpdateConsumerChart(RealTimeChartViewModel charts)
    {
        if (_consumerChart == null || _consSelector == null) return;

        int index = _consSelector.SelectedIndex;

        _consumerChart.Plot.PlottableList.Clear();

        if (index == 2)
        {
            void AddLatencyLine(MetricSeriesBuffer buffer, ScottPlot.Color color, string legend)
            {
                var scatter = _consumerChart.Plot.Add.Scatter(
                    buffer.XValues.ToArray(),
                    buffer.YValues.ToArray()
                );
                scatter.Color = color;
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
                scatter.LegendText = legend;
            }

            AddLatencyLine(charts.ConsumerLatencyP50, ChartTheme.LatencyP50, "P50");
            AddLatencyLine(charts.ConsumerLatencyP95, ChartTheme.LatencyP95, "P95");
            AddLatencyLine(charts.ConsumerLatencyP99, ChartTheme.LatencyP99, "P99");

            _consumerChart.Plot.ShowLegend(ScottPlot.Alignment.UpperLeft);
            _consumerChart.Plot.Axes.Left.Label.Text = "ms";
            _consumerChart.Plot.Title("Consumer E2E Latency (ms)", size: 12);
        }
        else
        {
            var buffer = index switch
            {
                1 => charts.ConsumerMsgRate,
                3 => charts.ConsumerErrors,
                _ => charts.ConsumerThroughput
            };
            var (color, label) = ChartTheme.GetMetricStyle(index);

            var scatter = _consumerChart.Plot.Add.Scatter(buffer.XValues.ToArray(), buffer.YValues.ToArray());
            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;

            _consumerChart.Plot.HideLegend();
            _consumerChart.Plot.Axes.Left.Label.Text = label;
            _consumerChart.Plot.Title($"{buffer.Title}", size: 12);
        }

        _consumerChart.Plot.Axes.AutoScale();
        _consumerChart.Refresh();
    }
}