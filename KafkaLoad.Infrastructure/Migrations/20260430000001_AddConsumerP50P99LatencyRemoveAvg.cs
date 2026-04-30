using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumerP50P99LatencyRemoveAvg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avg_e2e_latency_ms",
                table: "consumer_metrics");

            migrationBuilder.AddColumn<double>(
                name: "p50_e2e_latency_ms",
                table: "consumer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "p95_e2e_latency_ms",
                table: "consumer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "p99_e2e_latency_ms",
                table: "consumer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "p50_e2e_latency_ms",
                table: "consumer_metrics");

            migrationBuilder.DropColumn(
                name: "p95_e2e_latency_ms",
                table: "consumer_metrics");

            migrationBuilder.DropColumn(
                name: "p99_e2e_latency_ms",
                table: "consumer_metrics");

            migrationBuilder.AddColumn<double>(
                name: "avg_e2e_latency_ms",
                table: "consumer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
