using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191121_3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "Slots",
            //    table: "EndorsingRights");

            migrationBuilder.AddColumn<int>(
                name: "SlotCount",
                table: "EndorsingRights",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "BalanceUpdates",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlotCount",
                table: "EndorsingRights");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "BalanceUpdates");

            migrationBuilder.AddColumn<uint>(
                name: "Slots",
                table: "EndorsingRights",
                nullable: false,
                defaultValue: 0u);
        }
    }
}
