using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models.Reports;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.Infrastructure.Database.Repositories;

public class SqliteTestReportRepository : ITestReportRepository
{
    private readonly KafkaLoadDbContext _db;

    public SqliteTestReportRepository(KafkaLoadDbContext db)
    {
        _db = db;
    }

    public async Task SaveReportAsync(TestReport report)
    {
        int? scenarioId = null;
        if (!string.IsNullOrEmpty(report.ScenarioName))
        {
            var scenario = await _db.TestScenarios
                .FirstOrDefaultAsync(x => x.Name == report.ScenarioName);
            scenarioId = scenario?.Id;
        }

        var entity = new TestReportEntity
        {
            Id = report.Id,
            CreatedAt = report.CreatedAt,
            ScenarioId = scenarioId,
            ScenarioName = report.ScenarioName,
            TestType = report.TestType.ToString(),
            DurationSeconds = report.DurationSeconds,
            TopicName = report.ConfigSnapshot.TopicName,
            ProducersCount = report.ConfigSnapshot.ProducersCount,
            ConsumersCount = report.ConfigSnapshot.ConsumersCount,
            MessageSizeBytes = report.ConfigSnapshot.MessageSize,
            TargetThroughput = report.ConfigSnapshot.TargetThroughput,
            ProducerMetrics = new ProducerMetricsEntity
            {
                ReportId = report.Id,
                TotalAttempted = report.ProducerMetrics.TotalMessagesAttempted,
                SuccessSent = report.ProducerMetrics.SuccessMessagesSent,
                ErrorCount = report.ProducerMetrics.ErrorMessages,
                TotalBytesSent = report.ProducerMetrics.TotalBytesSent,
                ErrorRatePercent = report.ProducerMetrics.ErrorRatePercent,
                ThroughputMsgSec = report.ProducerMetrics.ThroughputMsgSec,
                ThroughputBytesSec = report.ProducerMetrics.ThroughputBytesSec,
                AvgLatencyMs = report.ProducerMetrics.AvgLatencyMs,
                MaxLatencyMs = report.ProducerMetrics.MaxLatencyMs,
                P95LatencyMs = report.ProducerMetrics.P95Lat
            },
            ConsumerMetrics = report.ConsumerMetrics is null ? null : new ConsumerMetricsEntity
            {
                ReportId = report.Id,
                TotalConsumed = report.ConsumerMetrics.TotalMessagesConsumed,
                SuccessConsumed = report.ConsumerMetrics.SuccessMessagesConsumed,
                ErrorCount = report.ConsumerMetrics.ErrorMessagesConsumed,
                TotalBytesConsumed = report.ConsumerMetrics.TotalBytesConsumed,
                ThroughputMsgSec = report.ConsumerMetrics.ThroughputMsgSec,
                ThroughputBytesSec = report.ConsumerMetrics.ThroughputBytesSec,
                AvgE2ELatencyMs = report.ConsumerMetrics.AvgEndToEndLatencyMs,
                MaxE2ELatencyMs = report.ConsumerMetrics.MaxEndToEndLatencyMs,
                MaxConsumerLag = report.ConsumerMetrics.MaxConsumerLag,
                FinalConsumerLag = report.ConsumerMetrics.FinalConsumerLag
            },
            TimeSeriesPoints = report.TimeSeriesData
                .SelectMany(kvp => kvp.Value.Select(p => new TimeSeriesPointEntity
                {
                    ReportId = report.Id,
                    SeriesName = kvp.Key,
                    TimeSeconds = p.TimeSeconds,
                    Value = p.Value
                }))
                .ToList()
        };

        _db.TestReports.Add(entity);
        await _db.SaveChangesAsync();
        Log.Information("Saved test report {ReportId} for scenario '{ScenarioName}'", report.Id, report.ScenarioName);
    }

    public async Task<IEnumerable<TestReport>> GetAllReportsAsync()
    {
        var entities = await _db.TestReports
            .AsNoTracking()
            .Include(x => x.ProducerMetrics)
            .Include(x => x.ConsumerMetrics)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToDomain);
    }

    public async Task<Dictionary<string, List<TimeSeriesPoint>>> GetTimeSeriesDataAsync(string reportId)
    {
        var points = await _db.TimeSeriesPoints
            .AsNoTracking()
            .Where(p => p.ReportId == reportId)
            .ToListAsync();

        return points
            .GroupBy(p => p.SeriesName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new TimeSeriesPoint(p.TimeSeconds, p.Value)).ToList()
            );
    }

    public async Task DeleteReportAsync(string id)
    {
        var entity = await _db.TestReports.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            Log.Warning("Report not found for deletion: {ReportId}", id);
            return;
        }
        _db.TestReports.Remove(entity);
        await _db.SaveChangesAsync();
        Log.Information("Deleted report: {ReportId}", id);
    }

    public async Task<int> DeleteOldReportsAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var deleted = await _db.TestReports
            .Where(r => r.CreatedAt < cutoff)
            .ExecuteDeleteAsync();

        if (deleted > 0)
            Log.Information("Auto-cleanup: deleted {Count} reports older than {Days} days", deleted, olderThanDays);

        return deleted;
    }

    public async Task<IEnumerable<ScenarioRunSummary>> GetScenarioStatisticsAsync()
    {
        return await _db.Database
            .SqlQuery<ScenarioRunSummary>($"""
                SELECT r.scenario_name                        AS ScenarioName,
                       COUNT(r.id)                           AS RunCount,
                       MAX(r.created_at)                     AS LastRun,
                       COALESCE(AVG(pm.throughput_msg_sec), 0.0) AS AvgThroughputMsgSec
                FROM test_report r
                LEFT JOIN producer_metrics pm ON pm.report_id = r.id
                GROUP BY r.scenario_name
                ORDER BY LastRun DESC
                """)
            .ToListAsync();
    }

    private static TestReport MapToDomain(TestReportEntity e)
    {
        var timeSeriesData = e.TimeSeriesPoints
            .GroupBy(p => p.SeriesName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new TimeSeriesPoint(p.TimeSeconds, p.Value)).ToList()
            );

        return new TestReport
        {
            Id = e.Id,
            CreatedAt = e.CreatedAt,
            ScenarioName = e.ScenarioName,
            TestType = Enum.Parse<TestType>(e.TestType),
            DurationSeconds = e.DurationSeconds,
            ConfigSnapshot = new TestScenarioConfigSnapshot
            {
                TopicName = e.TopicName,
                ProducersCount = e.ProducersCount,
                ConsumersCount = e.ConsumersCount,
                MessageSize = e.MessageSizeBytes,
                TargetThroughput = e.TargetThroughput
            },
            ProducerMetrics = e.ProducerMetrics is null ? new ProducerReportMetrics() : new ProducerReportMetrics
            {
                TotalMessagesAttempted = e.ProducerMetrics.TotalAttempted,
                SuccessMessagesSent = e.ProducerMetrics.SuccessSent,
                ErrorMessages = e.ProducerMetrics.ErrorCount,
                TotalBytesSent = e.ProducerMetrics.TotalBytesSent,
                ErrorRatePercent = e.ProducerMetrics.ErrorRatePercent,
                ThroughputMsgSec = e.ProducerMetrics.ThroughputMsgSec,
                ThroughputBytesSec = e.ProducerMetrics.ThroughputBytesSec,
                AvgLatencyMs = e.ProducerMetrics.AvgLatencyMs,
                MaxLatencyMs = e.ProducerMetrics.MaxLatencyMs,
                P95Lat = e.ProducerMetrics.P95LatencyMs
            },
            ConsumerMetrics = e.ConsumerMetrics is null ? null : new ConsumerReportMetrics
            {
                TotalMessagesConsumed = e.ConsumerMetrics.TotalConsumed,
                SuccessMessagesConsumed = e.ConsumerMetrics.SuccessConsumed,
                ErrorMessagesConsumed = e.ConsumerMetrics.ErrorCount,
                TotalBytesConsumed = e.ConsumerMetrics.TotalBytesConsumed,
                ThroughputMsgSec = e.ConsumerMetrics.ThroughputMsgSec,
                ThroughputBytesSec = e.ConsumerMetrics.ThroughputBytesSec,
                AvgEndToEndLatencyMs = e.ConsumerMetrics.AvgE2ELatencyMs,
                MaxEndToEndLatencyMs = e.ConsumerMetrics.MaxE2ELatencyMs,
                MaxConsumerLag = e.ConsumerMetrics.MaxConsumerLag,
                FinalConsumerLag = e.ConsumerMetrics.FinalConsumerLag
            },
            TimeSeriesData = timeSeriesData
        };
    }
}
