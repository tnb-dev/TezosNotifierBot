using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TezosNotifyBot.Storage.Migrations
{
    /// <inheritdoc />
    public partial class Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_user_address_address",
                table: "user_address",
                column: "address");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_address_address",
                table: "user_address");
        }
    }
}
