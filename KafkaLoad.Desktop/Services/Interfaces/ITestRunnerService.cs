using KafkaLoad.Desktop.Models;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface ITestRunnerService
{
    Task RunTestAsync(TestScenario scenario);
    void StopTest();
    bool IsRunning { get; }
}
