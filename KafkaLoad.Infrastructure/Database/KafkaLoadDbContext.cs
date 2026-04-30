using KafkaLoad.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KafkaLoad.Infrastructure.Database;

public class KafkaLoadDbContext : DbContext
{
    public KafkaLoadDbContext(DbContextOptions<KafkaLoadDbContext> options) : base(options) { }

    public DbSet<ProducerConfigEntity> ProducerConfigs => Set<ProducerConfigEntity>();
    public DbSet<ConsumerConfigEntity> ConsumerConfigs => Set<ConsumerConfigEntity>();
    public DbSet<TestScenarioEntity> TestScenarios => Set<TestScenarioEntity>();
    public DbSet<TestReportEntity> TestReports => Set<TestReportEntity>();
    public DbSet<ProducerMetricsEntity> ProducerMetrics => Set<ProducerMetricsEntity>();
    public DbSet<ConsumerMetricsEntity> ConsumerMetrics => Set<ConsumerMetricsEntity>();
    public DbSet<TimeSeriesPointEntity> TimeSeriesPoints => Set<TimeSeriesPointEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProducerConfigEntity>(e =>
        {
            e.ToTable("producer_config");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.ClientId).HasColumnName("client_id").IsRequired();
            e.Property(x => x.Acks).HasColumnName("acks").IsRequired();
            e.Property(x => x.Retries).HasColumnName("retries");
            e.Property(x => x.EnableIdempotence).HasColumnName("enable_idempotence");
            e.Property(x => x.BatchSizeBytes).HasColumnName("batch_size_bytes");
            e.Property(x => x.LingerMs).HasColumnName("linger_ms");
            e.Property(x => x.CompressionType).HasColumnName("compression_type").IsRequired();
            e.Property(x => x.BufferMemoryBytes).HasColumnName("buffer_memory_bytes");
            e.Property(x => x.MaxInFlightRequests).HasColumnName("max_in_flight_requests");
            e.Property(x => x.AutoCreateTopicsEnable).HasColumnName("auto_create_topics_enable");
            e.Property(x => x.SecurityProtocol).HasColumnName("security_protocol").IsRequired();
            e.Property(x => x.SaslMechanism).HasColumnName("sasl_mechanism").IsRequired();
            e.Property(x => x.SaslUsername).HasColumnName("sasl_username");
            e.Property(x => x.SaslPassword).HasColumnName("sasl_password");
            e.Property(x => x.SslCaLocation).HasColumnName("ssl_ca_location");
            e.Property(x => x.SslCertificateLocation).HasColumnName("ssl_certificate_location");
            e.Property(x => x.SslKeyLocation).HasColumnName("ssl_key_location");
            e.Property(x => x.SslKeyPassword).HasColumnName("ssl_key_password");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ConsumerConfigEntity>(e =>
        {
            e.ToTable("consumer_config");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.AutoOffsetReset).HasColumnName("auto_offset_reset").IsRequired();
            e.Property(x => x.FetchMinBytes).HasColumnName("fetch_min_bytes");
            e.Property(x => x.FetchMaxBytes).HasColumnName("fetch_max_bytes");
            e.Property(x => x.FetchMaxWaitMs).HasColumnName("fetch_max_wait_ms");
            e.Property(x => x.MaxPollIntervalMs).HasColumnName("max_poll_interval_ms");
            e.Property(x => x.SecurityProtocol).HasColumnName("security_protocol").IsRequired();
            e.Property(x => x.SaslMechanism).HasColumnName("sasl_mechanism").IsRequired();
            e.Property(x => x.SaslUsername).HasColumnName("sasl_username");
            e.Property(x => x.SaslPassword).HasColumnName("sasl_password");
            e.Property(x => x.SslCaLocation).HasColumnName("ssl_ca_location");
            e.Property(x => x.SslCertificateLocation).HasColumnName("ssl_certificate_location");
            e.Property(x => x.SslKeyLocation).HasColumnName("ssl_key_location");
            e.Property(x => x.SslKeyPassword).HasColumnName("ssl_key_password");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<TestScenarioEntity>(e =>
        {
            e.ToTable("test_scenario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.ProducerConfigId).HasColumnName("producer_config_id");
            e.Property(x => x.ConsumerConfigId).HasColumnName("consumer_config_id");
            e.Property(x => x.KeyStrategy).HasColumnName("key_strategy").IsRequired();
            e.Property(x => x.ValueStrategy).HasColumnName("value_strategy").IsRequired();
            e.Property(x => x.FixedKey).HasColumnName("fixed_key");
            e.Property(x => x.FixedTemplate).HasColumnName("fixed_template");
            e.Property(x => x.MessageSizeBytes).HasColumnName("message_size_bytes");
            e.Property(x => x.TestType).HasColumnName("test_type").IsRequired();
            e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            e.Property(x => x.ProducerCount).HasColumnName("producer_count");
            e.Property(x => x.ConsumerCount).HasColumnName("consumer_count");
            e.Property(x => x.TargetThroughput).HasColumnName("target_throughput");
            e.Property(x => x.BaseThroughput).HasColumnName("base_throughput");
            e.Property(x => x.SpikeThroughput).HasColumnName("spike_throughput");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasOne(x => x.ProducerConfig)
                .WithMany(x => x.ScenariosAsProducer)
                .HasForeignKey(x => x.ProducerConfigId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.ConsumerConfig)
                .WithMany(x => x.ScenariosAsConsumer)
                .HasForeignKey(x => x.ConsumerConfigId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TestReportEntity>(e =>
        {
            e.ToTable("test_report");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ScenarioId).HasColumnName("scenario_id");
            e.Property(x => x.ScenarioName).HasColumnName("scenario_name").IsRequired();
            e.Property(x => x.TestType).HasColumnName("test_type").IsRequired();
            e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            e.Property(x => x.TopicName).HasColumnName("topic_name").IsRequired();
            e.Property(x => x.ProducersCount).HasColumnName("producers_count");
            e.Property(x => x.ConsumersCount).HasColumnName("consumers_count");
            e.Property(x => x.MessageSizeBytes).HasColumnName("message_size_bytes");
            e.Property(x => x.TargetThroughput).HasColumnName("target_throughput");

            e.HasOne(x => x.Scenario)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.ScenarioId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.ProducerMetrics)
                .WithOne(x => x.Report)
                .HasForeignKey<ProducerMetricsEntity>(x => x.ReportId);

            e.HasOne(x => x.ConsumerMetrics)
                .WithOne(x => x.Report)
                .HasForeignKey<ConsumerMetricsEntity>(x => x.ReportId);

            e.HasMany(x => x.TimeSeriesPoints)
                .WithOne(x => x.Report)
                .HasForeignKey(x => x.ReportId);
        });

        modelBuilder.Entity<ProducerMetricsEntity>(e =>
        {
            e.ToTable("producer_metrics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ReportId).HasColumnName("report_id").IsRequired();
            e.Property(x => x.TotalAttempted).HasColumnName("total_attempted");
            e.Property(x => x.SuccessSent).HasColumnName("success_sent");
            e.Property(x => x.ErrorCount).HasColumnName("error_count");
            e.Property(x => x.TotalBytesSent).HasColumnName("total_bytes_sent");
            e.Property(x => x.ErrorRatePercent).HasColumnName("error_rate_percent");
            e.Property(x => x.ThroughputMsgSec).HasColumnName("throughput_msg_sec");
            e.Property(x => x.ThroughputBytesSec).HasColumnName("throughput_bytes_sec");
            e.Property(x => x.MaxLatencyMs).HasColumnName("max_latency_ms");
            e.Property(x => x.P50LatencyMs).HasColumnName("p50_latency_ms");
            e.Property(x => x.P95LatencyMs).HasColumnName("p95_latency_ms");
            e.Property(x => x.P99LatencyMs).HasColumnName("p99_latency_ms");
        });

        modelBuilder.Entity<ConsumerMetricsEntity>(e =>
        {
            e.ToTable("consumer_metrics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ReportId).HasColumnName("report_id").IsRequired();
            e.Property(x => x.TotalConsumed).HasColumnName("total_consumed");
            e.Property(x => x.SuccessConsumed).HasColumnName("success_consumed");
            e.Property(x => x.ErrorCount).HasColumnName("error_count");
            e.Property(x => x.TotalBytesConsumed).HasColumnName("total_bytes_consumed");
            e.Property(x => x.ThroughputMsgSec).HasColumnName("throughput_msg_sec");
            e.Property(x => x.ThroughputBytesSec).HasColumnName("throughput_bytes_sec");
            e.Property(x => x.MaxE2ELatencyMs).HasColumnName("max_e2e_latency_ms");
            e.Property(x => x.P50E2ELatencyMs).HasColumnName("p50_e2e_latency_ms");
            e.Property(x => x.P95E2ELatencyMs).HasColumnName("p95_e2e_latency_ms");
            e.Property(x => x.P99E2ELatencyMs).HasColumnName("p99_e2e_latency_ms");
            e.Property(x => x.MaxConsumerLag).HasColumnName("max_consumer_lag");
            e.Property(x => x.FinalConsumerLag).HasColumnName("final_consumer_lag");
        });

        modelBuilder.Entity<TimeSeriesPointEntity>(e =>
        {
            e.ToTable("time_series_point");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ReportId).HasColumnName("report_id").IsRequired();
            e.Property(x => x.SeriesName).HasColumnName("series_name").IsRequired();
            e.Property(x => x.TimeSeconds).HasColumnName("time_seconds");
            e.Property(x => x.Value).HasColumnName("value");
            e.HasIndex(x => x.ReportId);
        });
    }
}
