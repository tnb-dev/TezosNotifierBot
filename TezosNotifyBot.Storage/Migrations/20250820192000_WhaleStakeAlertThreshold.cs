using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TezosNotifyBot.Storage.Migrations
{
    /// <inheritdoc />
    public partial class WhaleStakeAlertThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "whale_stake_alert_threshold",
                table: "user",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "whale_stake_alert_threshold",
                table: "user");
        }
    }
}
