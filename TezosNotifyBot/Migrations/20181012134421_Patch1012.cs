using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch1012 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyCycleCompletion",
                table: "UserAddresses",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "Accured",
                table: "DelegateRewards",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyCycleCompletion",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "Accured",
                table: "DelegateRewards");
        }
    }
}
