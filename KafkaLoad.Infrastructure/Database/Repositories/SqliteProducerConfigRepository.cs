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

public class SqliteProducerConfigRepository : IConfigRepository<CustomProducerConfig>
{
    private readonly KafkaLoadDbContext _db;

    public SqliteProducerConfigRepository(KafkaLoadDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CustomProducerConfig>> GetAllAsync()
    {
        var entities = await _db.ProducerConfigs.AsNoTracking().ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<CustomProducerConfig?> GetByNameAsync(string name)
    {
        var entity = await _db.ProducerConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(CustomProducerConfig config)
    {
        var existing = await _db.ProducerConfigs.FirstOrDefaultAsync(x => x.Name == config.Name);

        if (existing is null)
        {
            var entity = MapToEntity(config);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.ProducerConfigs.Add(entity);
            Log.Information("Inserting new producer config: {Name}", config.Name);
        }
        else
        {
            MapToEntity(config, existing);
            existing.UpdatedAt = DateTime.UtcNow;
            Log.Information("Updating existing producer config: {Name}", config.Name);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string name)
    {
        var entity = await _db.ProducerConfigs.FirstOrDefaultAsync(x => x.Name == name);
        if (entity is null)
        {
            Log.Warning("Producer config not found for deletion: {Name}", name);
            return;
        }
        _db.ProducerConfigs.Remove(entity);
        await _db.SaveChangesAsync();
        Log.Information("Deleted producer config: {Name}", name);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _db.ProducerConfigs.AnyAsync(x => x.Name == name);
    }

    private static CustomProducerConfig MapToDomain(ProducerConfigEntity e) => new()
    {
        Name = e.Name,
        BootstrapServers = e.BootstrapServers,
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

    private static ProducerConfigEntity MapToEntity(CustomProducerConfig c, ProducerConfigEntity? target = null)
    {
        target ??= new ProducerConfigEntity();
        target.Name = c.Name;
        target.BootstrapServers = c.BootstrapServers;
        target.ClientId = c.ClientID;
        target.Acks = c.Acks.ToString();
        target.Retries = c.Retries;
        target.EnableIdempotence = c.EnableIdempotence;
        target.BatchSizeBytes = c.BatchSize;
        target.LingerMs = c.Linger;
        target.CompressionType = c.CompressionType.ToString();
        target.BufferMemoryBytes = c.BufferMemory;
        target.MaxInFlightRequests = c.MaxInFlightRequestsPerConnection;
        target.AutoCreateTopicsEnable = c.AutoCreateTopicsEnable;
        target.SecurityProtocol = c.Security.SecurityProtocol.ToString();
        target.SaslMechanism = c.Security.SaslMechanism.ToString();
        target.SaslUsername = c.Security.SaslUsername;
        target.SaslPassword = c.Security.SaslPassword;
        target.SslCaLocation = c.Security.SslCaLocation;
        target.SslCertificateLocation = c.Security.SslCertificateLocation;
        target.SslKeyLocation = c.Security.SslKeyLocation;
        target.SslKeyPassword = c.Security.SslKeyPassword;
        return target;
    }
}
