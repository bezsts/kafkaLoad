using System.Collections.Generic;

namespace KafkaLoad.Desktop.Services.Visualization
{
    public class MetricSeriesBuffer
    {
        private readonly int _maxPoints;

        public List<double> XValues { get; } = new();
        public List<double> YValues { get; } = new();

        public string Title { get; }

        public MetricSeriesBuffer(string title, int maxPoints = 300)
        {
            Title = title;
            _maxPoints = maxPoints;
        }

        public void AddPoint(double timeSeconds, double value)
        {
            XValues.Add(timeSeconds);
            YValues.Add(value);

            if (XValues.Count > _maxPoints)
            {
                XValues.RemoveAt(0);
                YValues.RemoveAt(0);
            }
        }

        public void Clear()
        {
            XValues.Clear();
            YValues.Clear();
        }
    }
}
