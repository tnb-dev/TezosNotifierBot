using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch0808 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyBakingRewards",
                table: "UserAddresses",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyEndorsementRewards",
                table: "UserAddresses",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyBakingRewards",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NotifyEndorsementRewards",
                table: "UserAddresses");
        }
    }
}
