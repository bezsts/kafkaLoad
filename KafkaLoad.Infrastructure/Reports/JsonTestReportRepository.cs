using KafkaLoad.Core.Models.Reports;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KafkaLoad.Infrastructure.Reports
{
    public class JsonTestReportRepository : ITestReportRepository
    {
        private readonly string _reportsDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonTestReportRepository()
        {
            _reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

            if (!Directory.Exists(_reportsDirectory))
            {
                Log.Information("Creating base reports directory: {Directory}", _reportsDirectory);
                Directory.CreateDirectory(_reportsDirectory);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true // Make it human-readable
            };
        }

        public async Task SaveReportAsync(TestReport report)
        {
            string fileName = $"report_{report.CreatedAt:yyyyMMdd_HHmmss}_{report.Id}.json";
            string filePath = Path.Combine(_reportsDirectory, fileName);

            try
            {
                Log.Information("Saving test report '{ScenarioName}' to {FilePath}", report.ScenarioName, filePath);

                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, report, _jsonOptions);

                Log.Debug("Successfully saved report: {ReportId}", report.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save test report {ReportId} to {FilePath}", report.Id, filePath);
                throw;
            }
        }

        public async Task<IEnumerable<TestReport>> GetAllReportsAsync()
        {
            var reports = new List<TestReport>();

            if (!Directory.Exists(_reportsDirectory))
            {
                Log.Debug("Reports directory does not exist, returning empty list.");
                return reports;
            }

            var files = Directory.GetFiles(_reportsDirectory, "report_*.json");
            Log.Information("Found {Count} report files in directory. Starting deserialization.", files.Length);

            foreach (var file in files)
            {
                try
                {
                    using var stream = File.OpenRead(file);
                    var report = await JsonSerializer.DeserializeAsync<TestReport>(stream, _jsonOptions);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                    else
                    {
                        Log.Warning("Deserialization returned null for file: {File}", file);
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Failed to parse JSON in report file: {File}. File might be corrupted.", file);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error reading report file: {File}", file);
                }
            }

            Log.Information("Successfully loaded {Count} test reports.", reports.Count);
            return reports.OrderByDescending(r => r.CreatedAt);
        }

        public Task<Dictionary<string, List<TimeSeriesPoint>>> GetTimeSeriesDataAsync(string reportId)
        {
            // JSON-based reports store time series inline — return empty; caller reads the full report via GetAllReportsAsync
            return Task.FromResult(new Dictionary<string, List<TimeSeriesPoint>>());
        }

        public Task<int> DeleteOldReportsAsync(int olderThanDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
            int deleted = 0;

            var files = Directory.Exists(_reportsDirectory)
                ? Directory.GetFiles(_reportsDirectory, "report_*.json")
                : Array.Empty<string>();

            foreach (var file in files)
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }

            if (deleted > 0)
                Log.Information("Auto-cleanup: deleted {Count} JSON reports older than {Days} days", deleted, olderThanDays);

            return Task.FromResult(deleted);
        }

        public Task<IEnumerable<ScenarioRunSummary>> GetScenarioStatisticsAsync()
        {
            // Statistics are not supported in the JSON-based repository
            return Task.FromResult(Enumerable.Empty<ScenarioRunSummary>());
        }

        public Task DeleteReportAsync(string id)
        {
            Log.Information("Attempting to delete report with ID: {ReportId}", id);

            try
            {
                var files = Directory.GetFiles(_reportsDirectory, $"*_{id}.json");

                if (files.Length == 0)
                {
                    Log.Warning("No files found to delete for report ID: {ReportId}", id);
                    return Task.CompletedTask;
                }

                foreach (var file in files)
                {
                    File.Delete(file);
                    Log.Debug("Deleted report file: {File}", file);
                }

                Log.Information("Successfully deleted report with ID: {ReportId}", id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while attempting to delete report with ID: {ReportId}", id);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
