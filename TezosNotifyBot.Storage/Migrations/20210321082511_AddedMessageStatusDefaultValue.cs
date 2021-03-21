using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class AddedMessageStatusDefaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "message",
                type: "integer",
                nullable: false,
                defaultValueSql: "0",
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "message",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "0");
        }
    }
}
