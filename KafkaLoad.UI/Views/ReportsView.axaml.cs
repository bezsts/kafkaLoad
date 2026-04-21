using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using KafkaLoad.UI.ViewModels.Reports;
using KafkaLoad.UI.Helpers;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot.Avalonia;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables.Fluent;
using KafkaLoad.Core.Models.Reports;

namespace KafkaLoad.UI.Views;

public partial class ReportsView : ReactiveUserControl<ReportsViewModel>
{
    // Producer Charts
    private AvaPlot? _prodThrMb, _prodMsgRate, _prodLat, _prodErrors;

    // Consumer Charts
    private AvaPlot? _consThrMb, _consMsgRate, _consLat, _consErrors;

    public ReportsView()
    {
        InitializeComponent();

        _prodThrMb   = this.FindControl<AvaPlot>("ProdThroughputMbChart");
        _prodMsgRate = this.FindControl<AvaPlot>("ProdMsgRateChart");
        _prodLat     = this.FindControl<AvaPlot>("ProdLatencyChart");
        _prodErrors  = this.FindControl<AvaPlot>("ProdErrorsChart");

        _consThrMb   = this.FindControl<AvaPlot>("ConsThroughputMbChart");
        _consMsgRate = this.FindControl<AvaPlot>("ConsMsgRateChart");
        _consLat     = this.FindControl<AvaPlot>("ConsLatencyChart");
        _consErrors  = this.FindControl<AvaPlot>("ConsErrorsChart");

        this.WhenActivated(disposables =>
        {
            ViewModel!.SaveFileDialog.RegisterHandler(async ctx =>
            {
                var topLevel = TopLevel.GetTopLevel(this)!;
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export Test Report",
                    SuggestedFileName = ctx.Input,
                    FileTypeChoices = [new FilePickerFileType("HTML File") { Patterns = ["*.html"] }]
                });
                ctx.SetOutput(file?.Path.LocalPath);
            });

            SetupChartStyle(_prodThrMb);
            SetupChartStyle(_prodMsgRate);
            SetupChartStyle(_prodLat);
            SetupChartStyle(_prodErrors);
            SetupChartStyle(_consThrMb);
            SetupChartStyle(_consMsgRate);
            SetupChartStyle(_consLat);
            SetupChartStyle(_consErrors);

            this.WhenAnyValue(x => x.ViewModel!.SelectedReport, x => x.ViewModel!.CompareWithReport)
                .Subscribe(tuple =>
                {
                    var (report, compare) = tuple;
                    if (compare == null)
                        UpdateAllCharts(report);
                    else
                        UpdateAllComparisonCharts(report, compare);
                })
                .DisposeWith(disposables);

            // WhenAnyValue uses DistinctUntilChanged and won't re-fire when the same report
            // reference gets its TimeSeriesData populated. Subscribe to the dedicated signal instead.
            ViewModel!.TimeSeriesReady
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(report =>
                {
                    if (ViewModel?.CompareWithReport == null)
                        UpdateAllCharts(report);
                    else
                        UpdateAllComparisonCharts(report, ViewModel.CompareWithReport);
                })
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

        DrawChart(_prodThrMb,   report, "Producer_ThroughputMB", "MB/s",    0);
        DrawChart(_prodMsgRate, report, "Producer_MsgRate",       "msg/s",   0);
        DrawChart(_prodLat,     report, "Producer_Latency",       "ms",      2);
        DrawChart(_prodErrors,  report, "Producer_Errors",        "err/s",   3);

        DrawChart(_consThrMb,   report, "Consumer_ThroughputMB", "MB/s",    0);
        DrawChart(_consMsgRate, report, "Consumer_MsgRate",       "msg/s",   0);
        DrawChart(_consLat,     report, "Consumer_Latency",       "ms",      2);
        DrawChart(_consErrors,  report, "Consumer_Errors",        "err/s",   3);
    }

    private void DrawChart(AvaPlot? chart, TestReport report, string dictionaryKey, string yLabel, int themeIndex)
    {
        if (chart == null) return;

        chart.Plot.PlottableList.Clear();

        if (report.TimeSeriesData != null && report.TimeSeriesData.TryGetValue(dictionaryKey, out var points) && points.Count > 0)
        {
            var xs = points.Select(p => p.TimeSeconds).ToArray();
            var ys = points.Select(p => p.Value).ToArray();

            var (color, _) = ChartTheme.GetMetricStyle(themeIndex);

            var scatter = chart.Plot.Add.Scatter(xs, ys);
            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;
        }

        chart.Plot.Axes.Left.Label.Text = yLabel;
        chart.Plot.Title(dictionaryKey.Replace("_", " "), size: 12);
        chart.Plot.HideLegend();
        chart.Plot.Axes.AutoScale();
        chart.Refresh();
    }

    private void UpdateAllComparisonCharts(TestReport? r1, TestReport? r2)
    {
        if (r1 == null || r2 == null) return;

        DrawComparisonChart(_prodThrMb,   r1, r2, "Producer_ThroughputMB", "MB/s",  lowerIsBetter: false, 0);
        DrawComparisonChart(_prodMsgRate, r1, r2, "Producer_MsgRate",       "msg/s", lowerIsBetter: false, 0);
        DrawComparisonChart(_prodLat,     r1, r2, "Producer_Latency",       "ms",    lowerIsBetter: true,  2);
        DrawComparisonChart(_prodErrors,  r1, r2, "Producer_Errors",        "err/s", lowerIsBetter: true,  3);

        DrawComparisonChart(_consThrMb,   r1, r2, "Consumer_ThroughputMB", "MB/s",  lowerIsBetter: false, 0);
        DrawComparisonChart(_consMsgRate, r1, r2, "Consumer_MsgRate",       "msg/s", lowerIsBetter: false, 0);
        DrawComparisonChart(_consLat,     r1, r2, "Consumer_Latency",       "ms",    lowerIsBetter: true,  2);
        DrawComparisonChart(_consErrors,  r1, r2, "Consumer_Errors",        "err/s", lowerIsBetter: true,  3);
    }

    private void DrawComparisonChart(AvaPlot? chart, TestReport r1, TestReport r2, string key, string yLabel, bool lowerIsBetter, int themeIndex)
    {
        if (chart == null) return;
        chart.Plot.PlottableList.Clear();

        if (r1.TimeSeriesData == null || r2.TimeSeriesData == null ||
            !r1.TimeSeriesData.TryGetValue(key, out var p1) || !r2.TimeSeriesData.TryGetValue(key, out var p2) ||
            p1.Count == 0 || p2.Count == 0)
        {
            chart.Refresh();
            return;
        }

        int count = Math.Min(p1.Count, p2.Count);

        var xs = new double[count];
        var ysOriginal = new double[count];
        var ysCompared = new double[count];

        for (int i = 0; i < count; i++)
        {
            xs[i] = p1[i].TimeSeconds;
            ysOriginal[i] = p1[i].Value;
            ysCompared[i] = p2[i].Value;
        }

        var colorGreen = ScottPlot.Color.FromHex("#16A34A40");
        var colorRed = ScottPlot.Color.FromHex("#EF444440");

        void AddPolygon(double x1, double x2, double y1_orig, double y2_orig, double y1_comp, double y2_comp, double diff)
        {
            if (Math.Abs(diff) < 0.0001) return;

            bool isBetter = lowerIsBetter ? diff < 0 : diff > 0;

            var poly = chart.Plot.Add.Polygon(new ScottPlot.Coordinates[] {
                new(x1, y1_orig),
                new(x2, y2_orig),
                new(x2, y2_comp),
                new(x1, y1_comp)
            });
            poly.FillColor = isBetter ? colorGreen : colorRed;
            poly.LineWidth = 0;
        }

        for (int i = 0; i < count - 1; i++)
        {
            double x1 = xs[i], x2 = xs[i + 1];
            double y1_orig = ysOriginal[i], y2_orig = ysOriginal[i + 1];
            double y1_comp = ysCompared[i], y2_comp = ysCompared[i + 1];

            double diff1 = y1_comp - y1_orig;
            double diff2 = y2_comp - y2_orig;

            if ((diff1 > 0 && diff2 < 0) || (diff1 < 0 && diff2 > 0))
            {
                double t = diff1 / (diff1 - diff2);
                double xInt = x1 + t * (x2 - x1);
                double yInt = y1_orig + t * (y2_orig - y1_orig);

                AddPolygon(x1, xInt, y1_orig, yInt, y1_comp, yInt, diff1);
                AddPolygon(xInt, x2, yInt, y2_orig, yInt, y2_comp, diff2);
            }
            else
            {
                AddPolygon(x1, x2, y1_orig, y2_orig, y1_comp, y2_comp, diff1 != 0 ? diff1 : diff2);
            }
        }

        var scatter1 = chart.Plot.Add.Scatter(xs, ysOriginal);
        scatter1.Color = ScottPlot.Color.FromHex("#9CA3AF");
        scatter1.LineWidth = 2f;
        scatter1.MarkerSize = 0;
        scatter1.LegendText = "Original";

        var (mainColor, _) = ChartTheme.GetMetricStyle(themeIndex);

        var scatter2 = chart.Plot.Add.Scatter(xs, ysCompared);
        scatter2.Color = mainColor;
        scatter2.LineWidth = 2.5f;
        scatter2.MarkerSize = 0;
        scatter2.LegendText = "Compared";

        chart.Plot.ShowLegend(ScottPlot.Alignment.UpperRight);
        chart.Plot.Axes.Left.Label.Text = yLabel;
        chart.Plot.Title(key.Replace("_", " ") + " Diff", size: 12);
        chart.Plot.Axes.AutoScale();
        chart.Refresh();
    }
}
