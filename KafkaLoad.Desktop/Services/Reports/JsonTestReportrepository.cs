using KafkaLoad.Desktop.Models.Reports;
using KafkaLoad.Desktop.Services.Reports.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Reports
{
    public class JsonTestReportRepository : ITestReportRepository
    {
        private readonly string _reportsDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonTestReportRepository()
        {
            // Save reports in a "Reports" folder near the executable
            _reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

            if (!Directory.Exists(_reportsDirectory))
            {
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

            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, report, _jsonOptions);
        }

        public async Task<IEnumerable<TestReport>> GetAllReportsAsync()
        {
            var reports = new List<TestReport>();

            if (!Directory.Exists(_reportsDirectory)) return reports;

            var files = Directory.GetFiles(_reportsDirectory, "report_*.json");

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
                }
                catch
                {
                    // Skip corrupted files
                }
            }

            return reports.OrderByDescending(r => r.CreatedAt);
        }

        public Task DeleteReportAsync(string id)
        {
            var files = Directory.GetFiles(_reportsDirectory, $"*_{id}.json");
            foreach (var file in files)
            {
                File.Delete(file);
            }
            return Task.CompletedTask;
        }
    }
}
