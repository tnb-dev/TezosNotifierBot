using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EditUserAddressId",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountThreshold",
                table: "UserAddresses",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditUserAddressId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AmountThreshold",
                table: "UserAddresses");
        }
    }
}
