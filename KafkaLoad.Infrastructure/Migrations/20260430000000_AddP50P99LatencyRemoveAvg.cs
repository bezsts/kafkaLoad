using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddP50P99LatencyRemoveAvg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avg_latency_ms",
                table: "producer_metrics");

            migrationBuilder.AddColumn<double>(
                name: "p50_latency_ms",
                table: "producer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "p99_latency_ms",
                table: "producer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "p50_latency_ms",
                table: "producer_metrics");

            migrationBuilder.DropColumn(
                name: "p99_latency_ms",
                table: "producer_metrics");

            migrationBuilder.AddColumn<double>(
                name: "avg_latency_ms",
                table: "producer_metrics",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
