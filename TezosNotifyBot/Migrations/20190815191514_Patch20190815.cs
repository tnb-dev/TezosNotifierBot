using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20190815 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WhaleAlertThreshold",
                table: "Users",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhaleAlertThreshold",
                table: "Users");
        }
    }
}
