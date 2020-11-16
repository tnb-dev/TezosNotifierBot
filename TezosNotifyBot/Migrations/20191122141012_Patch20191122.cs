using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191122 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Slots",
                table: "BalanceUpdates",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slots",
                table: "BalanceUpdates");
        }
    }
}
