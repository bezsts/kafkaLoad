using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaLoad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGroupIdFromConsumerConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "group_id",
                table: "consumer_config");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_id",
                table: "consumer_config",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
