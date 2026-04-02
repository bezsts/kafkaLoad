namespace KafkaLoad.Infrastructure.Database.Entities;

public class TimeSeriesPointEntity
{
    public int Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public TestReportEntity Report { get; set; } = null!;

    public string SeriesName { get; set; } = string.Empty;
    public double TimeSeconds { get; set; }
    public double Value { get; set; }
}
