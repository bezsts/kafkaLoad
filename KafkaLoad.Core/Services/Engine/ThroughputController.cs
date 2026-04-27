using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Engine;

public class ThroughputController : IDisposable
{
    private readonly TestType _testType;
    private readonly int _durationSec;
    private readonly double _targetRps;
    private readonly double _baseRps;
    private readonly double _spikeRps;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
    private double _fractionalTokens;
    private Stopwatch? _stopwatch;

    public ThroughputController(TestScenario scenario)
    {
        _testType = scenario.TestType ?? throw new InvalidOperationException("TestType must be set before running a test.");
        _durationSec = scenario.Duration ?? 60;
        _targetRps = scenario.TargetThroughput ?? 1000;
        _baseRps = scenario.BaseThroughput ?? 100;
        _spikeRps = scenario.SpikeThroughput ?? (_targetRps * 5);

        Log.Information("ThroughputController initialized for {TestType} test. Duration: {Duration}s. Target: {TargetRps} msg/s. Base: {BaseRps} msg/s. Spike: {SpikeRps} msg/s.",
            _testType, _durationSec, _targetRps, _baseRps, _spikeRps);
    }

    public async Task WaitForTokenAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
    }

    public async Task RunReplenisherAsync(CancellationToken ct)
    {
        Log.Information("Throughput token replenisher started.");

        _stopwatch = Stopwatch.StartNew();
        double lastTickSec = 0;

        try
        {
            // Run until cancelled or test duration expires
            while (!ct.IsCancellationRequested && _stopwatch.Elapsed.TotalSeconds <= _durationSec)
            {
                double currentSec = _stopwatch.Elapsed.TotalSeconds;
                double deltaTime = currentSec - lastTickSec;
                lastTickSec = currentSec;

                double currentTargetRps = CalculateCurrentRps(currentSec);

                double accumulated = currentTargetRps * deltaTime + _fractionalTokens;
                int tokensToAdd = (int)accumulated;
                _fractionalTokens = accumulated - tokensToAdd;

                if (tokensToAdd > 0)
                {
                    int currentCount = _semaphore.CurrentCount;
                    int maxTokens = (int)Math.Ceiling(currentTargetRps);
                    int allowed = Math.Min(tokensToAdd, Math.Max(0, maxTokens - currentCount));

                    if (allowed > 0)
                        _semaphore.Release(allowed);
                }

                // Refill the bucket every 10ms for a smooth data flow
                await Task.Delay(10, ct);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("Throughput token replenisher cancelled cleanly.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in Throughput token replenisher loop.");
        }
        finally
        {
            _stopwatch.Stop();
            Log.Information("Throughput token replenisher stopped. Total time run: {ElapsedSeconds:N2}s", _stopwatch.Elapsed.TotalSeconds);
        }
    }


    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private double CalculateCurrentRps(double currentSec)
    {
        // Calculate test progress as a value from 0.0 to 1.0 (0% to 100%)
        double p = currentSec / _durationSec;
        if (p > 1.0) p = 1.0;

        switch (_testType)
        {
            case TestType.Load:
                // Image 1: Classic Trapezoid (20% Ramp-up, 60% Hold, 20% Ramp-down)
                if (p < 0.20)
                    return _targetRps * (p / 0.20); // Ramp-up
                else if (p <= 0.80)
                    return _targetRps;              // Steady State
                else
                    return _targetRps * ((1.0 - p) / 0.20); // Ramp-down

            case TestType.Stress:
                // Image 2: Step-up load (4 steps up, then ramp-down)
                // 0% to 90% is for the 4 steps. 90% to 100% is for the final ramp-down.
                if (p > 0.90)
                {
                    return _targetRps * ((1.0 - p) / 0.10); // Final Ramp-down
                }

                int totalSteps = 4;
                double timePerStep = 0.90 / totalSteps; // 0.225 (22.5%) per step

                int currentStep = (int)(p / timePerStep);
                if (currentStep >= totalSteps) currentStep = totalSteps - 1;

                // localP represents progress within the current step (0.0 to 1.0)
                double localP = (p - (currentStep * timePerStep)) / timePerStep;

                double prevTarget = _targetRps * currentStep / totalSteps;
                double stepTarget = _targetRps * (currentStep + 1) / totalSteps;

                // First half of the step is ramping up, second half is holding the value
                if (localP < 0.50)
                {
                    // Ramping up to the next step
                    return prevTarget + (stepTarget - prevTarget) * (localP / 0.50);
                }
                else
                {
                    // Holding the step
                    return stepTarget;
                }

            case TestType.Spike:
                // Image 3: Base -> Fast Spike -> Base -> Down
                // 0.00-0.05: Ramp up to Base
                // 0.05-0.20: Hold Base
                // 0.20-0.25: Sharp ramp up to Spike
                // 0.25-0.55: Hold Spike (Peak)
                // 0.55-0.60: Sharp ramp down to Base
                // 0.60-0.95: Hold Base
                // 0.95-1.00: Ramp down to 0
                if (p < 0.05)
                    return _baseRps * (p / 0.05);
                else if (p < 0.20)
                    return _baseRps;
                else if (p < 0.25)
                    return _baseRps + (_spikeRps - _baseRps) * ((p - 0.20) / 0.05);
                else if (p < 0.55)
                    return _spikeRps;
                else if (p < 0.60)
                    return _spikeRps - (_spikeRps - _baseRps) * ((p - 0.55) / 0.05);
                else if (p < 0.95)
                    return _baseRps;
                else
                    return _baseRps * ((1.0 - p) / 0.05);

            case TestType.Soak:
                // Image 4: Fast ramp-up, long plateau, fast ramp-down (2% / 96% / 2%)
                if (p < 0.02)
                    return _targetRps * (p / 0.02); // Fast Ramp-up
                else if (p <= 0.98)
                    return _targetRps;              // Long Plateau
                else
                    return _targetRps * ((1.0 - p) / 0.02); // Fast Ramp-down

            default:
                return _targetRps;
        }
    }
}