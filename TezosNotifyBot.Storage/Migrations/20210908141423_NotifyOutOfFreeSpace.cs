using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class NotifyOutOfFreeSpace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "notify_out_of_free_space",
                table: "user_address",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notify_out_of_free_space",
                table: "user_address");
        }
    }
}
