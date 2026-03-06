using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Models.Reports
{
    public class MetricDiff
    {
        public string MetricName { get; set; } = string.Empty;
        public string Value1 { get; set; } = string.Empty;
        public string Value2 { get; set; } = string.Empty;

        public string DiffText { get; set; } = string.Empty;
        public string DiffColor { get; set; } = "#9CA3AF";
    }
}
