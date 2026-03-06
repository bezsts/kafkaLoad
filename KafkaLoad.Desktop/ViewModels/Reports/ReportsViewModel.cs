using KafkaLoad.Desktop.Models.Reports;
using KafkaLoad.Desktop.Services.Reports.Interfaces;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.ViewModels.Reports
{
    public class ReportsViewModel : ReactiveObject
    {
        private readonly ITestReportRepository _reportRepository;

        public ObservableCollection<TestReport> Reports { get; } = new();

        private TestReport? _selectedReport;
        public TestReport? SelectedReport
        {
            get => _selectedReport;
            set => this.RaiseAndSetIfChanged(ref _selectedReport, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private TestReport? _compareWithReport;
        public TestReport? CompareWithReport
        {
            get => _compareWithReport;
            set
            {
                this.RaiseAndSetIfChanged(ref _compareWithReport, value);
                GenerateComparison(); // Recalculate diffs when second report is selected
            }
        }

        public ObservableCollection<MetricDiff> ProducerComparison { get; } = new();
        public ObservableCollection<MetricDiff> ConsumerComparison { get; } = new();

        public ReactiveCommand<Unit, Unit> LoadReportsCommand { get; }
        public ReactiveCommand<string, Unit> DeleteReportCommand { get; }

        public ReportsViewModel(ITestReportRepository reportRepository)
        {
            _reportRepository = reportRepository;

            LoadReportsCommand = ReactiveCommand.CreateFromTask(LoadReportsAsync);

            DeleteReportCommand = ReactiveCommand.CreateFromTask<string>(DeleteReportAsync);

            LoadReportsCommand.Execute().Subscribe();
        }

        private async Task LoadReportsAsync()
        {
            IsLoading = true;
            try
            {
                var reports = await _reportRepository.GetAllReportsAsync();

                Reports.Clear();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading reports: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteReportAsync(string id)
        {
            await _reportRepository.DeleteReportAsync(id);

            if (SelectedReport?.Id == id)
            {
                SelectedReport = null;
            }

            await LoadReportsAsync();
        }

        private void GenerateComparison()
        {
            ProducerComparison.Clear();
            ConsumerComparison.Clear();

            if (SelectedReport == null || CompareWithReport == null) return;

            // --- PRODUCER METRICS ---
            var p1 = SelectedReport.ProducerMetrics;
            var p2 = CompareWithReport.ProducerMetrics;

            ProducerComparison.Add(Compare("Success Sent", p1.SuccessMessagesSent, p2.SuccessMessagesSent, lowerIsBetter: false, "N0"));
            ProducerComparison.Add(Compare("Errors", p1.ErrorMessages, p2.ErrorMessages, lowerIsBetter: true, "N0"));
            ProducerComparison.Add(Compare("Throughput (Msg/s)", p1.ThroughputMsgSec, p2.ThroughputMsgSec, lowerIsBetter: false));
            ProducerComparison.Add(Compare("Throughput (MB/s)", p1.ThroughputBytesSec / 1024.0 / 1024.0, p2.ThroughputBytesSec / 1024.0 / 1024.0, lowerIsBetter: false));
            ProducerComparison.Add(Compare("Avg Latency (ms)", p1.AvgLatencyMs, p2.AvgLatencyMs, lowerIsBetter: true));
            ProducerComparison.Add(Compare("P95 Latency (ms)", p1.P95Lat, p2.P95Lat, lowerIsBetter: true));

            // --- CONSUMER METRICS ---
            if (SelectedReport.ConsumerMetrics != null && CompareWithReport.ConsumerMetrics != null)
            {
                var c1 = SelectedReport.ConsumerMetrics;
                var c2 = CompareWithReport.ConsumerMetrics;

                ConsumerComparison.Add(Compare("Success Recv", c1.SuccessMessagesConsumed, c2.SuccessMessagesConsumed, lowerIsBetter: false, "N0"));
                ConsumerComparison.Add(Compare("Throughput (Msg/s)", c1.ThroughputMsgSec, c2.ThroughputMsgSec, lowerIsBetter: false));
                ConsumerComparison.Add(Compare("Throughput (MB/s)", c1.ThroughputBytesSec / 1024.0 / 1024.0, c2.ThroughputBytesSec / 1024.0 / 1024.0, lowerIsBetter: false));
                ConsumerComparison.Add(Compare("Avg E2E Latency (ms)", c1.AvgEndToEndLatencyMs, c2.AvgEndToEndLatencyMs, lowerIsBetter: true));
                ConsumerComparison.Add(Compare("Max Lag", c1.MaxConsumerLag, c2.MaxConsumerLag, lowerIsBetter: true, "N0"));
            }
        }

        // Helper method to calculate differences and assign colors
        private MetricDiff Compare(string name, double v1, double v2, bool lowerIsBetter, string format = "N2")
        {
            double diff = v2 - v1;
            double pct = v1 != 0 ? (diff / v1) * 100.0 : 0;

            string sign = diff > 0 ? "+" : "";
            string diffText = diff == 0 ? "No change" : $"{sign}{diff.ToString(format)} ({sign}{pct:N1}%)";

            string color = "#9CA3AF";
            if (diff != 0)
            {
                bool isImprovement = lowerIsBetter ? diff < 0 : diff > 0;
                color = isImprovement ? "#16A34A" : "#EF4444";
            }

            return new MetricDiff
            {
                MetricName = name,
                Value1 = v1.ToString(format),
                Value2 = v2.ToString(format),
                DiffText = diffText,
                DiffColor = color
            };
        }
    }
}
