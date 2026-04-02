using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumer_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    bootstrap_servers = table.Column<string>(type: "TEXT", nullable: false),
                    group_id = table.Column<string>(type: "TEXT", nullable: false),
                    auto_offset_reset = table.Column<string>(type: "TEXT", nullable: false),
                    fetch_min_bytes = table.Column<int>(type: "INTEGER", nullable: false),
                    fetch_max_bytes = table.Column<int>(type: "INTEGER", nullable: false),
                    fetch_max_wait_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    max_poll_interval_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    security_protocol = table.Column<string>(type: "TEXT", nullable: false),
                    sasl_mechanism = table.Column<string>(type: "TEXT", nullable: false),
                    sasl_username = table.Column<string>(type: "TEXT", nullable: true),
                    sasl_password = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_ca_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_certificate_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_key_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_key_password = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumer_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producer_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    bootstrap_servers = table.Column<string>(type: "TEXT", nullable: false),
                    client_id = table.Column<string>(type: "TEXT", nullable: false),
                    acks = table.Column<string>(type: "TEXT", nullable: false),
                    retries = table.Column<int>(type: "INTEGER", nullable: false),
                    enable_idempotence = table.Column<bool>(type: "INTEGER", nullable: false),
                    batch_size_bytes = table.Column<int>(type: "INTEGER", nullable: false),
                    linger_ms = table.Column<double>(type: "REAL", nullable: false),
                    compression_type = table.Column<string>(type: "TEXT", nullable: false),
                    buffer_memory_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    max_in_flight_requests = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_create_topics_enable = table.Column<bool>(type: "INTEGER", nullable: false),
                    security_protocol = table.Column<string>(type: "TEXT", nullable: false),
                    sasl_mechanism = table.Column<string>(type: "TEXT", nullable: false),
                    sasl_username = table.Column<string>(type: "TEXT", nullable: true),
                    sasl_password = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_ca_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_certificate_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_key_location = table.Column<string>(type: "TEXT", nullable: true),
                    ssl_key_password = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producer_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_scenario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    topic_name = table.Column<string>(type: "TEXT", nullable: false),
                    producer_config_id = table.Column<int>(type: "INTEGER", nullable: true),
                    consumer_config_id = table.Column<int>(type: "INTEGER", nullable: true),
                    key_strategy = table.Column<string>(type: "TEXT", nullable: false),
                    value_strategy = table.Column<string>(type: "TEXT", nullable: false),
                    fixed_template = table.Column<string>(type: "TEXT", nullable: true),
                    message_size_bytes = table.Column<int>(type: "INTEGER", nullable: true),
                    test_type = table.Column<string>(type: "TEXT", nullable: false),
                    duration_seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    producer_count = table.Column<int>(type: "INTEGER", nullable: true),
                    consumer_count = table.Column<int>(type: "INTEGER", nullable: true),
                    target_throughput = table.Column<int>(type: "INTEGER", nullable: true),
                    base_throughput = table.Column<int>(type: "INTEGER", nullable: true),
                    spike_throughput = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_scenario", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_scenario_consumer_config_consumer_config_id",
                        column: x => x.consumer_config_id,
                        principalTable: "consumer_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_test_scenario_producer_config_producer_config_id",
                        column: x => x.producer_config_id,
                        principalTable: "producer_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "test_report",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    scenario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    scenario_name = table.Column<string>(type: "TEXT", nullable: false),
                    test_type = table.Column<string>(type: "TEXT", nullable: false),
                    duration_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    topic_name = table.Column<string>(type: "TEXT", nullable: false),
                    producers_count = table.Column<int>(type: "INTEGER", nullable: false),
                    consumers_count = table.Column<int>(type: "INTEGER", nullable: false),
                    message_size_bytes = table.Column<int>(type: "INTEGER", nullable: false),
                    target_throughput = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_report", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_report_test_scenario_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "test_scenario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "consumer_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    report_id = table.Column<string>(type: "TEXT", nullable: false),
                    total_consumed = table.Column<long>(type: "INTEGER", nullable: false),
                    success_consumed = table.Column<long>(type: "INTEGER", nullable: false),
                    error_count = table.Column<long>(type: "INTEGER", nullable: false),
                    total_bytes_consumed = table.Column<long>(type: "INTEGER", nullable: false),
                    throughput_msg_sec = table.Column<double>(type: "REAL", nullable: false),
                    throughput_bytes_sec = table.Column<double>(type: "REAL", nullable: false),
                    avg_e2e_latency_ms = table.Column<double>(type: "REAL", nullable: false),
                    max_e2e_latency_ms = table.Column<double>(type: "REAL", nullable: false),
                    max_consumer_lag = table.Column<long>(type: "INTEGER", nullable: false),
                    final_consumer_lag = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumer_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_consumer_metrics_test_report_report_id",
                        column: x => x.report_id,
                        principalTable: "test_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producer_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    report_id = table.Column<string>(type: "TEXT", nullable: false),
                    total_attempted = table.Column<long>(type: "INTEGER", nullable: false),
                    success_sent = table.Column<long>(type: "INTEGER", nullable: false),
                    error_count = table.Column<long>(type: "INTEGER", nullable: false),
                    total_bytes_sent = table.Column<long>(type: "INTEGER", nullable: false),
                    error_rate_percent = table.Column<double>(type: "REAL", nullable: false),
                    throughput_msg_sec = table.Column<double>(type: "REAL", nullable: false),
                    throughput_bytes_sec = table.Column<double>(type: "REAL", nullable: false),
                    avg_latency_ms = table.Column<double>(type: "REAL", nullable: false),
                    max_latency_ms = table.Column<double>(type: "REAL", nullable: false),
                    p95_latency_ms = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producer_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_producer_metrics_test_report_report_id",
                        column: x => x.report_id,
                        principalTable: "test_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_series_point",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    report_id = table.Column<string>(type: "TEXT", nullable: false),
                    series_name = table.Column<string>(type: "TEXT", nullable: false),
                    time_seconds = table.Column<double>(type: "REAL", nullable: false),
                    value = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_series_point", x => x.id);
                    table.ForeignKey(
                        name: "FK_time_series_point_test_report_report_id",
                        column: x => x.report_id,
                        principalTable: "test_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consumer_config_name",
                table: "consumer_config",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consumer_metrics_report_id",
                table: "consumer_metrics",
                column: "report_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_producer_config_name",
                table: "producer_config",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_producer_metrics_report_id",
                table: "producer_metrics",
                column: "report_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_report_scenario_id",
                table: "test_report",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_scenario_consumer_config_id",
                table: "test_scenario",
                column: "consumer_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_scenario_name",
                table: "test_scenario",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_scenario_producer_config_id",
                table: "test_scenario",
                column: "producer_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_series_point_report_id",
                table: "time_series_point",
                column: "report_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumer_metrics");

            migrationBuilder.DropTable(
                name: "producer_metrics");

            migrationBuilder.DropTable(
                name: "time_series_point");

            migrationBuilder.DropTable(
                name: "test_report");

            migrationBuilder.DropTable(
                name: "test_scenario");

            migrationBuilder.DropTable(
                name: "consumer_config");

            migrationBuilder.DropTable(
                name: "producer_config");
        }
    }
}
