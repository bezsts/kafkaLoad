using ScottPlot;

namespace KafkaLoad.Desktop.Configuration
{
    public static class ChartTheme
    {
        public static readonly Color Background = Color.FromHex("#FFFFFF");
        public static readonly Color GridLines = Color.FromHex("#E2E8F0");
        public static readonly Color Text = Color.FromHex("#64748B");

        public static readonly Color MetricThroughputMB = Color.FromHex("#0EA5E9");
        public static readonly Color MetricThroughputMsg = Color.FromHex("#8B5CF6");
        public static readonly Color MetricLatency = Color.FromHex("#F59E0B");
        public static readonly Color MetricErrors = Color.FromHex("#EF4444");

        public static (Color Color, string Label) GetMetricStyle(int index)
        {
            return index switch
            {
                1 => (MetricThroughputMsg, "Msg/sec"),
                2 => (MetricLatency, "ms"),
                3 => (MetricErrors, "Errors"),
                _ => (MetricThroughputMB, "MB/s")
            };
        }
    }
}
