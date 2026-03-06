using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.Configuration;
using KafkaLoad.Desktop.Models.Reports;
using KafkaLoad.Desktop.ViewModels.Reports;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot.Avalonia;
using System;
using System.Linq;
using System.Reactive.Disposables.Fluent;

namespace KafkaLoad.Desktop.Views;

public partial class ReportsView : ReactiveUserControl<ReportsViewModel>
{
    // Producer Charts
    private AvaPlot? _prodThrMb, _prodThrMsg, _prodLat, _prodErr;

    // Consumer Charts
    private AvaPlot? _consThrMb, _consThrMsg, _consLat, _consErr;

    public ReportsView()
    {
        InitializeComponent();

        // Bind Producer Charts
        _prodThrMb = this.FindControl<AvaPlot>("ProdThroughputMbChart");
        _prodThrMsg = this.FindControl<AvaPlot>("ProdThroughputMsgChart");
        _prodLat = this.FindControl<AvaPlot>("ProdLatencyChart");
        _prodErr = this.FindControl<AvaPlot>("ProdErrorsChart");

        // Bind Consumer Charts
        _consThrMb = this.FindControl<AvaPlot>("ConsThroughputMbChart");
        _consThrMsg = this.FindControl<AvaPlot>("ConsThroughputMsgChart");
        _consLat = this.FindControl<AvaPlot>("ConsLatencyChart");
        _consErr = this.FindControl<AvaPlot>("ConsErrorsChart");

        this.WhenActivated(disposables =>
        {
            // Initialize basic styles for all 8 charts
            SetupChartStyle(_prodThrMb);
            SetupChartStyle(_prodThrMsg);
            SetupChartStyle(_prodLat);
            SetupChartStyle(_prodErr);

            SetupChartStyle(_consThrMb);
            SetupChartStyle(_consThrMsg);
            SetupChartStyle(_consLat);
            SetupChartStyle(_consErr);

            // Redraw everything when a new report is selected
            this.WhenAnyValue(x => x.ViewModel.SelectedReport)
                .Subscribe(report => UpdateAllCharts(report))
                .DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupChartStyle(AvaPlot? chart)
    {
        if (chart == null) return;

        chart.Plot.Clear();
        chart.Plot.FigureBackground.Color = ChartTheme.Background;
        chart.Plot.DataBackground.Color = ChartTheme.Background;
        chart.Plot.Axes.Color(ChartTheme.Text);
        chart.Plot.Grid.MajorLineColor = ChartTheme.GridLines;
        chart.Refresh();

        chart.PointerWheelChanged += (sender, e) =>
        {
            e.Handled = true;
        };
    }

    private void UpdateAllCharts(TestReport? report)
    {
        if (report == null) return;

        // Draw Producer Charts
        // Theme indexes matching ChartTheme.GetMetricStyle: 0=MB/s, 1=Msg/s, 2=Latency, 3=Errors
        DrawChart(_prodThrMb, report, "Producer_ThroughputMB", "MB/s", 0);
        DrawChart(_prodThrMsg, report, "Producer_MsgRate", "Msg/s", 1);
        DrawChart(_prodLat, report, "Producer_Latency", "ms", 2);
        DrawChart(_prodErr, report, "Producer_Errors", "Errors/s", 3);

        // Draw Consumer Charts
        DrawChart(_consThrMb, report, "Consumer_ThroughputMB", "MB/s", 0);
        DrawChart(_consThrMsg, report, "Consumer_MsgRate", "Msg/s", 1);
        DrawChart(_consLat, report, "Consumer_Latency", "ms", 2);
        DrawChart(_consErr, report, "Consumer_Errors", "Errors/s", 3);
    }

    // A universal method to extract data, style the line, and render it
    private void DrawChart(AvaPlot? chart, TestReport report, string dictionaryKey, string yLabel, int themeIndex)
    {
        if (chart == null) return;

        // Clear previous lines
        chart.Plot.PlottableList.Clear();

        if (report.TimeSeriesData != null && report.TimeSeriesData.TryGetValue(dictionaryKey, out var points) && points.Count > 0)
        {
            // Extract X and Y arrays
            var xs = points.Select(p => p.TimeSeconds).ToArray();
            var ys = points.Select(p => p.Value).ToArray();

            // Get standard color from the theme
            var (color, _) = ChartTheme.GetMetricStyle(themeIndex);

            var scatter = chart.Plot.Add.Scatter(xs, ys);
            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;
        }

        // Apply titles and labels
        chart.Plot.Axes.Left.Label.Text = yLabel;

        // Remove underscore for the title (e.g., "Producer_Latency" -> "Producer Latency")
        chart.Plot.Title(dictionaryKey.Replace("_", " "), size: 12);

        chart.Plot.Axes.AutoScale();
        chart.Refresh();
    }
}