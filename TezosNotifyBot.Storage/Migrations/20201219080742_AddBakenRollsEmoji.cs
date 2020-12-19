using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class AddBakenRollsEmoji : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "address_config",
                columns: new[] { "id", "icon" },
                values: new object[] { "tz1NortRftucvAkD1J58L32EhSVrQEWJCEnB", "🥨" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "address_config",
                keyColumn: "id",
                keyValue: "tz1NortRftucvAkD1J58L32EhSVrQEWJCEnB");
        }
    }
}
