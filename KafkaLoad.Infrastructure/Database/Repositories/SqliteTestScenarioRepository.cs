using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Configs;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.Infrastructure.Database.Repositories;

public class SqliteTestScenarioRepository : IConfigRepository<TestScenario>
{
    private readonly KafkaLoadDbContext _db;

    public SqliteTestScenarioRepository(KafkaLoadDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TestScenario>> GetAllAsync()
    {
        var entities = await _db.TestScenarios
            .AsNoTracking()
            .Include(x => x.ProducerConfig)
            .Include(x => x.ConsumerConfig)
            .ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<TestScenario?> GetByNameAsync(string name)
    {
        var entity = await _db.TestScenarios
            .AsNoTracking()
            .Include(x => x.ProducerConfig)
            .Include(x => x.ConsumerConfig)
            .FirstOrDefaultAsync(x => x.Name == name);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(TestScenario scenario)
    {
        int? producerConfigId = scenario.ProducerConfig?.Id > 0 ? scenario.ProducerConfig.Id : null;
        int? consumerConfigId = scenario.ConsumerConfig?.Id > 0 ? scenario.ConsumerConfig.Id : null;

        var existing = await _db.TestScenarios.FirstOrDefaultAsync(x => x.Name == scenario.Name);

        if (existing is null)
        {
            var entity = MapToEntity(scenario);
            entity.ProducerConfigId = producerConfigId;
            entity.ConsumerConfigId = consumerConfigId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.TestScenarios.Add(entity);
            Log.Information("Inserting new test scenario: {Name}", scenario.Name);
        }
        else
        {
            MapToEntity(scenario, existing);
            existing.ProducerConfigId = producerConfigId;
            existing.ConsumerConfigId = consumerConfigId;
            existing.UpdatedAt = DateTime.UtcNow;
            Log.Information("Updating existing test scenario: {Name}", scenario.Name);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string name)
    {
        var entity = await _db.TestScenarios.FirstOrDefaultAsync(x => x.Name == name);
        if (entity is null)
        {
            Log.Warning("Test scenario not found for deletion: {Name}", name);
            return;
        }
        _db.TestScenarios.Remove(entity);
        await _db.SaveChangesAsync();
        Log.Information("Deleted test scenario: {Name}", name);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _db.TestScenarios.AnyAsync(x => x.Name == name);
    }

    public async Task RenameAndSaveAsync(string originalName, TestScenario config)
    {
        var entity = await _db.TestScenarios.FirstOrDefaultAsync(x => x.Name == originalName);
        if (entity is null)
        {
            await SaveAsync(config);
            return;
        }
        entity.Name = config.Name;
        entity.UpdatedAt = DateTime.UtcNow;
        Log.Information("Renaming test scenario '{OldName}' → '{NewName}'", originalName, config.Name);
        await _db.SaveChangesAsync();
    }

    private static TestScenario MapToDomain(TestScenarioEntity e) => new()
    {
        Name = e.Name,
        KeyStrategy = Enum.Parse<KeyGenerationStrategy>(e.KeyStrategy),
        ValueStrategy = Enum.Parse<ValueGenerationStrategy>(e.ValueStrategy),
        FixedKey = e.FixedKey,
        FixedTemplate = e.FixedTemplate,
        MessageSize = e.MessageSizeBytes,
        TestType = Enum.Parse<TestType>(e.TestType),
        Duration = e.DurationSeconds,
        ProducerCount = e.ProducerCount,
        ConsumerCount = e.ConsumerCount,
        TargetThroughput = e.TargetThroughput,
        BaseThroughput = e.BaseThroughput,
        SpikeThroughput = e.SpikeThroughput,
        CreatedAt = e.CreatedAt,
        ProducerConfig = e.ProducerConfig is null ? null : MapProducerConfig(e.ProducerConfig),
        ConsumerConfig = e.ConsumerConfig is null ? null : MapConsumerConfig(e.ConsumerConfig)
    };

    private static CustomProducerConfig MapProducerConfig(ProducerConfigEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        ClientID = e.ClientId,
        Acks = Enum.Parse<AcksEnum>(e.Acks),
        Retries = e.Retries,
        EnableIdempotence = e.EnableIdempotence,
        BatchSize = e.BatchSizeBytes,
        Linger = e.LingerMs,
        CompressionType = Enum.Parse<CompressionTypeEnum>(e.CompressionType),
        BufferMemory = e.BufferMemoryBytes,
        MaxInFlightRequestsPerConnection = e.MaxInFlightRequests,
        Security = new CustomSecurityConfig
        {
            SecurityProtocol = Enum.Parse<SecurityProtocolEnum>(e.SecurityProtocol),
            SaslMechanism = Enum.Parse<SaslMechanismEnum>(e.SaslMechanism),
            SaslUsername = e.SaslUsername,
            SaslPassword = e.SaslPassword,
            SslCaLocation = e.SslCaLocation,
            SslCertificateLocation = e.SslCertificateLocation,
            SslKeyLocation = e.SslKeyLocation,
            SslKeyPassword = e.SslKeyPassword
        }
    };

    private static CustomConsumerConfig MapConsumerConfig(ConsumerConfigEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        GroupId = e.GroupId,
        AutoOffsetReset = Enum.Parse<AutoOffsetResetEnum>(e.AutoOffsetReset),
        FetchMinBytes = e.FetchMinBytes,
        FetchMaxBytes = e.FetchMaxBytes,
        FetchMaxWait = e.FetchMaxWaitMs,
        MaxPollInterval = e.MaxPollIntervalMs,
        Security = new CustomSecurityConfig
        {
            SecurityProtocol = Enum.Parse<SecurityProtocolEnum>(e.SecurityProtocol),
            SaslMechanism = Enum.Parse<SaslMechanismEnum>(e.SaslMechanism),
            SaslUsername = e.SaslUsername,
            SaslPassword = e.SaslPassword,
            SslCaLocation = e.SslCaLocation,
            SslCertificateLocation = e.SslCertificateLocation,
            SslKeyLocation = e.SslKeyLocation,
            SslKeyPassword = e.SslKeyPassword
        }
    };

    private static TestScenarioEntity MapToEntity(TestScenario s, TestScenarioEntity? target = null)
    {
        target ??= new TestScenarioEntity();
        target.Name = s.Name;
        target.KeyStrategy = s.KeyStrategy!.Value.ToString();
        target.ValueStrategy = s.ValueStrategy!.Value.ToString();
        target.FixedKey = s.FixedKey;
        target.FixedTemplate = s.FixedTemplate;
        target.MessageSizeBytes = s.MessageSize;
        target.TestType = s.TestType!.Value.ToString();
        target.DurationSeconds = s.Duration;
        target.ProducerCount = s.ProducerCount;
        target.ConsumerCount = s.ConsumerCount;
        target.TargetThroughput = s.TargetThroughput;
        target.BaseThroughput = s.BaseThroughput;
        target.SpikeThroughput = s.SpikeThroughput;
        return target;
    }
}
