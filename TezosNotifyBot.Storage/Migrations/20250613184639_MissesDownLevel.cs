using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TezosNotifyBot.Storage.Migrations
{
    /// <inheritdoc />
    public partial class MissesDownLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "down_end_level",
                table: "user_address",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "down_start_level",
                table: "user_address",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "down_end_level",
                table: "user_address");

            migrationBuilder.DropColumn(
                name: "down_start_level",
                table: "user_address");
        }
    }
}
