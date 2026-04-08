using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBootstrapAndTopicFromConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "topic_name",
                table: "test_scenario");

            migrationBuilder.DropColumn(
                name: "bootstrap_servers",
                table: "producer_config");

            migrationBuilder.DropColumn(
                name: "bootstrap_servers",
                table: "consumer_config");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "topic_name",
                table: "test_scenario",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bootstrap_servers",
                table: "producer_config",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bootstrap_servers",
                table: "consumer_config",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
