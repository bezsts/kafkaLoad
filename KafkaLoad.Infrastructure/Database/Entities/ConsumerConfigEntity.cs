using System;
using System.Collections.Generic;

namespace KafkaLoad.Infrastructure.Database.Entities;

public class ConsumerConfigEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string AutoOffsetReset { get; set; } = string.Empty;
    public int FetchMinBytes { get; set; }
    public int FetchMaxBytes { get; set; }
    public int FetchMaxWaitMs { get; set; }
    public int MaxPollIntervalMs { get; set; }

    // Security
    public string SecurityProtocol { get; set; } = string.Empty;
    public string SaslMechanism { get; set; } = string.Empty;
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? SslCaLocation { get; set; }
    public string? SslCertificateLocation { get; set; }
    public string? SslKeyLocation { get; set; }
    public string? SslKeyPassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TestScenarioEntity> ScenariosAsConsumer { get; set; } = new List<TestScenarioEntity>();
}
