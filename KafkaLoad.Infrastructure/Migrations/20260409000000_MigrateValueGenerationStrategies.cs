using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateValueGenerationStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fixed → Json: fixed_template content becomes the literal JSON template.
            migrationBuilder.Sql(
                "UPDATE test_scenario SET value_strategy = 'Json' WHERE value_strategy = 'Fixed';");

            // RandomJson → RandomString: the old generator used message_size_bytes anyway.
            migrationBuilder.Sql(
                "UPDATE test_scenario SET value_strategy = 'RandomString', message_size_bytes = COALESCE(message_size_bytes, 1024) WHERE value_strategy = 'RandomJson';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE test_scenario SET value_strategy = 'Fixed' WHERE value_strategy = 'Json';");

            migrationBuilder.Sql(
                "UPDATE test_scenario SET value_strategy = 'RandomJson' WHERE value_strategy = 'RandomString';");
        }
    }
}
