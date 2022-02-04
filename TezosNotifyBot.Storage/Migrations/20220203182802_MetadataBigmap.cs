using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class MetadataBigmap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "metadata_bigmap",
                table: "token",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "metadata_bigmap",
                table: "token");
        }
    }
}
