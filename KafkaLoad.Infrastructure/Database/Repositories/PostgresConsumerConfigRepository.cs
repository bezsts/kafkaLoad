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

public class PostgresConsumerConfigRepository : IConfigRepository<CustomConsumerConfig>
{
    private readonly KafkaLoadDbContext _db;

    public PostgresConsumerConfigRepository(KafkaLoadDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CustomConsumerConfig>> GetAllAsync()
    {
        var entities = await _db.ConsumerConfigs.AsNoTracking().ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<CustomConsumerConfig?> GetByNameAsync(string name)
    {
        var entity = await _db.ConsumerConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(CustomConsumerConfig config)
    {
        var existing = await _db.ConsumerConfigs.FirstOrDefaultAsync(x => x.Name == config.Name);

        if (existing is null)
        {
            var entity = MapToEntity(config);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.ConsumerConfigs.Add(entity);
            Log.Information("Inserting new consumer config: {Name}", config.Name);
        }
        else
        {
            MapToEntity(config, existing);
            existing.UpdatedAt = DateTime.UtcNow;
            Log.Information("Updating existing consumer config: {Name}", config.Name);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string name)
    {
        var entity = await _db.ConsumerConfigs.FirstOrDefaultAsync(x => x.Name == name);
        if (entity is null)
        {
            Log.Warning("Consumer config not found for deletion: {Name}", name);
            return;
        }
        _db.ConsumerConfigs.Remove(entity);
        await _db.SaveChangesAsync();
        Log.Information("Deleted consumer config: {Name}", name);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _db.ConsumerConfigs.AnyAsync(x => x.Name == name);
    }

    private static CustomConsumerConfig MapToDomain(ConsumerConfigEntity e) => new()
    {
        Name = e.Name,
        BootstrapServers = e.BootstrapServers,
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

    private static ConsumerConfigEntity MapToEntity(CustomConsumerConfig c, ConsumerConfigEntity? target = null)
    {
        target ??= new ConsumerConfigEntity();
        target.Name = c.Name;
        target.BootstrapServers = c.BootstrapServers;
        target.GroupId = c.GroupId;
        target.AutoOffsetReset = c.AutoOffsetReset.ToString();
        target.FetchMinBytes = c.FetchMinBytes;
        target.FetchMaxBytes = c.FetchMaxBytes;
        target.FetchMaxWaitMs = c.FetchMaxWait;
        target.MaxPollIntervalMs = c.MaxPollInterval;
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
