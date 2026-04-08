using System;
using System.Collections.Generic;

namespace KafkaLoad.Infrastructure.Database.Entities;

public class TestScenarioEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int? ProducerConfigId { get; set; }
    public ProducerConfigEntity? ProducerConfig { get; set; }

    public int? ConsumerConfigId { get; set; }
    public ConsumerConfigEntity? ConsumerConfig { get; set; }

    public string KeyStrategy { get; set; } = string.Empty;
    public string ValueStrategy { get; set; } = string.Empty;
    public string? FixedTemplate { get; set; }
    public int? MessageSizeBytes { get; set; }
    public string TestType { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }
    public int? ProducerCount { get; set; }
    public int? ConsumerCount { get; set; }
    public int? TargetThroughput { get; set; }
    public int? BaseThroughput { get; set; }
    public int? SpikeThroughput { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TestReportEntity> Reports { get; set; } = new List<TestReportEntity>();
}
