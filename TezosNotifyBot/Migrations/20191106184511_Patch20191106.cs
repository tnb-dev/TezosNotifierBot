using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191106 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProposalVotes",
                columns: table => new
                {
                    ProposalVoteID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegateId = table.Column<int>(nullable: false),
                    ProposalID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalVotes", x => x.ProposalVoteID);
                    table.ForeignKey(
                        name: "FK_ProposalVotes_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalVotes_Proposals_ProposalID",
                        column: x => x.ProposalID,
                        principalTable: "Proposals",
                        principalColumn: "ProposalID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalVotes_DelegateId",
                table: "ProposalVotes",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalVotes_ProposalID",
                table: "ProposalVotes",
                column: "ProposalID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposalVotes");
        }
    }
}
