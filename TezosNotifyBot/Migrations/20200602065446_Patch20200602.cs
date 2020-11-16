using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20200602 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DelegationAmountThreshold",
                table: "UserAddresses",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMisses",
                table: "UserAddresses",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelegationAmountThreshold",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NotifyMisses",
                table: "UserAddresses");
        }
    }
}
