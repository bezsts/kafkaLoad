using System;
using System.Collections.Generic;

namespace KafkaLoad.Infrastructure.Database.Entities;

public class ProducerConfigEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    // Acks
    public string Acks { get; set; } = string.Empty;

    // Retries
    public int Retries { get; set; }

    public bool EnableIdempotence { get; set; }
    public int BatchSizeBytes { get; set; }
    public double LingerMs { get; set; }
    public string CompressionType { get; set; } = string.Empty;
    public long BufferMemoryBytes { get; set; }
    public int MaxInFlightRequests { get; set; }
    public bool AutoCreateTopicsEnable { get; set; }

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

    public ICollection<TestScenarioEntity> ScenariosAsProducer { get; set; } = new List<TestScenarioEntity>();
}
