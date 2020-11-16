using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20200420 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ballot",
                table: "ProposalVotes",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "ProposalVotes",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ballot",
                table: "ProposalVotes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "ProposalVotes");
        }
    }
}
