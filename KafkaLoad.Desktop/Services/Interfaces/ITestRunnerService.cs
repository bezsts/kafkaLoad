using System;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Models;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface ITestRunnerService
{
    Task RunTestAsync(TestScenario scenario);
    void StopTest();
    bool IsRunning { get; }
}
