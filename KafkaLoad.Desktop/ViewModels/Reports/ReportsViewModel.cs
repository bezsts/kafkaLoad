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
    }
}
