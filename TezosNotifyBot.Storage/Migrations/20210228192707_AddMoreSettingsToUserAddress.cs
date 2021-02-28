using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class AddMoreSettingsToUserAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "delegators_balance_threshold",
                table: "user_address",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "notify_delegators_balance",
                table: "user_address",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_payout",
                table: "user_address",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delegators_balance_threshold",
                table: "user_address");

            migrationBuilder.DropColumn(
                name: "notify_delegators_balance",
                table: "user_address");

            migrationBuilder.DropColumn(
                name: "notify_payout",
                table: "user_address");
        }
    }
}
