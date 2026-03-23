using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Interfaces;

public interface ITestRunnerService
{
    Task RunTestAsync(TestScenario scenario);
    Task GenerateAndSaveReportAsync(TestScenario scenario, Dictionary<string, List<TimeSeriesPoint>> timeSeriesData);
    void StopTest();
    bool IsRunning { get; }
}
