using System.Collections.Generic;

namespace KafkaLoad.UI.Helpers;

/// <summary>
/// Calculates a smoothed rate/average over a sliding window of recent samples.
///
/// For rate metrics (msg/s, MB/s): Add(deltaCount, deltaTimeSec)
///   → returns sum(deltaCount) / sum(deltaTimeSec) over the window
///
/// For weighted averages (latency): Add(value * weight, weight)
///   → returns weighted average over the window
/// </summary>
public class SlidingWindowRate
{
    private readonly Queue<(double numerator, double denominator)> _window;
    private readonly int _maxSamples;
    private double _totalNumerator;
    private double _totalDenominator;

    public SlidingWindowRate(int maxSamples)
    {
        _maxSamples = maxSamples;
        _window = new Queue<(double, double)>(maxSamples + 1);
    }

    public double Add(double numerator, double denominator)
    {
        _window.Enqueue((numerator, denominator));
        _totalNumerator += numerator;
        _totalDenominator += denominator;

        if (_window.Count > _maxSamples)
        {
            var (oldNum, oldDen) = _window.Dequeue();
            _totalNumerator -= oldNum;
            _totalDenominator -= oldDen;
        }

        return _totalDenominator > 0 ? _totalNumerator / _totalDenominator : 0;
    }

    public void Reset()
    {
        _window.Clear();
        _totalNumerator = 0;
        _totalDenominator = 0;
    }
}
