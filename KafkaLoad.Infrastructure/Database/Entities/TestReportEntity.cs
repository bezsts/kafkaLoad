using System;
using System.Collections.Generic;

namespace KafkaLoad.Infrastructure.Database.Entities;

public class TestReportEntity
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public int? ScenarioId { get; set; }
    public TestScenarioEntity? Scenario { get; set; }

    // Snapshot of scenario state at run time
    public string ScenarioName { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int ProducersCount { get; set; }
    public int ConsumersCount { get; set; }
    public int MessageSizeBytes { get; set; }
    public double? TargetThroughput { get; set; }

    public ProducerMetricsEntity? ProducerMetrics { get; set; }
    public ConsumerMetricsEntity? ConsumerMetrics { get; set; }
    public ICollection<TimeSeriesPointEntity> TimeSeriesPoints { get; set; } = new List<TimeSeriesPointEntity>();
}
