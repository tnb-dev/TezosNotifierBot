using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TezosNotifyBot.Storage.Migrations
{
    /// <inheritdoc />
    public partial class MaxAddrCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_addr_count",
                table: "user",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_addr_count",
                table: "user");
        }
    }
}
