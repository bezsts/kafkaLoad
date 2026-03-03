using KafkaLoad.Desktop.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Reports.Interfaces
{
    public interface ITestReportRepository
    {
        // Saves a new report to the storage (File/DB)
        Task SaveReportAsync(TestReport report);

        // Retrieves all reports for the History view
        Task<IEnumerable<TestReport>> GetAllReportsAsync();

        // Deletes a report
        Task DeleteReportAsync(string id);
    }
}
