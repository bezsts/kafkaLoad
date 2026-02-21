using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Workers;

public class ThroughputController
{
    private readonly TestType _testType;
    private readonly int _durationSec;
    private readonly double _targetRps;
    private readonly double _baseRps;
    private readonly double _spikeRps;

    private double _currentTokens;
    private readonly object _lockObj = new();
    private Stopwatch? _stopwatch;

    public ThroughputController(TestScenario scenario)
    {
        _testType = scenario.TestType;
        _durationSec = scenario.Duration ?? 60;
        _targetRps = scenario.TargetThroughput ?? 1000;
        _baseRps = scenario.BaseThroughput ?? 100;
        _spikeRps = scenario.SpikeThroughput ?? (_targetRps * 5);

        _currentTokens = 0;
    }

    public async Task WaitForTokenAsync(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            lock (_lockObj)
            {
                if (_currentTokens >= 1.0)
                {
                    // Take one token and allow the worker to send the message
                    _currentTokens -= 1.0;
                    return;
                }
            }

            // Wait 10ms before trying again. 
            // This prevents ThreadPool starvation and UI freezing.
            await Task.Delay(10, ct);
        }
    }

    public async Task RunReplenisherAsync(CancellationToken ct)
    {
        _stopwatch = Stopwatch.StartNew();
        double lastTickSec = 0;

        // Run until cancelled or test duration expires
        while (!ct.IsCancellationRequested && _stopwatch.Elapsed.TotalSeconds <= _durationSec)
        {
            double currentSec = _stopwatch.Elapsed.TotalSeconds;
            double deltaTime = currentSec - lastTickSec;
            lastTickSec = currentSec;

            double currentTargetRps = CalculateCurrentRps(currentSec);

            lock (_lockObj)
            {
                // Add new tokens based on the elapsed time and current RPS
                _currentTokens += currentTargetRps * deltaTime;

                // Cap the bucket size to 1 second of throughput to prevent massive bursts
                // if workers are momentarily slower than the token generation
                if (_currentTokens > currentTargetRps)
                {
                    _currentTokens = currentTargetRps;
                }
            }

            // Refill the bucket every 10ms for a smooth data flow
            await Task.Delay(10, ct);
        }
    }

    private double CalculateCurrentRps(double currentSec)
    {
        switch (_testType)
        {
            case TestType.Soak:
                // Ramp-up linearly for the first 5% of the duration
                double rampUpTime = _durationSec * 0.05;
                if (currentSec < rampUpTime)
                {
                    return (_targetRps / rampUpTime) * currentSec;
                }
                // Then hold the plateau
                return _targetRps;

            case TestType.Stress:
                // Increase linearly from 0 to max throughout the entire test
                return (_targetRps / _durationSec) * currentSec;

            case TestType.Spike:
                // Phase 1: Base load (0% to 20%)
                // Phase 2: Spike load (20% to 30%)
                // Phase 3: Base load (30% to 100%)
                double phase1End = _durationSec * 0.20;
                double phase2End = _durationSec * 0.30;

                if (currentSec > phase1End && currentSec <= phase2End)
                {
                    return _spikeRps;
                }
                return _baseRps;

            case TestType.Load:
            default:
                // Constant throughput
                return _targetRps;
        }
    }
}