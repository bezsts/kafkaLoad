using KafkaLoad.Core.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Interfaces
{
    public interface ITestReportRepository
    {
        Task SaveReportAsync(TestReport report);

        // Retrieves all reports without time series data (for the list view)
        Task<IEnumerable<TestReport>> GetAllReportsAsync();

        // Loads time series data for a single report on demand (for chart rendering)
        Task<Dictionary<string, List<TimeSeriesPoint>>> GetTimeSeriesDataAsync(string reportId);

        Task DeleteReportAsync(string id);

        // Removes reports older than the given number of days
        Task<int> DeleteOldReportsAsync(int olderThanDays);

        // Returns per-scenario run counts and average throughput (uses SQL aggregation)
        Task<IEnumerable<ScenarioRunSummary>> GetScenarioStatisticsAsync();
    }
}
