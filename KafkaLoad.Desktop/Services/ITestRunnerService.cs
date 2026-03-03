using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface ITestRunnerService
{
    Task RunTestAsync(TestScenario scenario);
    Task GenerateAndSaveReportAsync(TestScenario scenario, Dictionary<string, List<TimeSeriesPoint>> timeSeriesData);
    void StopTest();
    bool IsRunning { get; }
}
