using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191121 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BakingRights",
                columns: table => new
                {
                    BakingRightsID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegateId = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakingRights", x => x.BakingRightsID);
                    table.ForeignKey(
                        name: "FK_BakingRights_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BalanceUpdates",
                columns: table => new
                {
                    BalanceUpdateID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegateId = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceUpdates", x => x.BalanceUpdateID);
                    table.ForeignKey(
                        name: "FK_BalanceUpdates_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsingRights",
                columns: table => new
                {
                    EndorsingRightsID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegateId = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    Slots = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsingRights", x => x.EndorsingRightsID);
                    table.ForeignKey(
                        name: "FK_EndorsingRights_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_DelegateId",
                table: "BakingRights",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceUpdates_DelegateId",
                table: "BalanceUpdates",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsingRights_DelegateId",
                table: "EndorsingRights",
                column: "DelegateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BakingRights");

            migrationBuilder.DropTable(
                name: "BalanceUpdates");

            migrationBuilder.DropTable(
                name: "EndorsingRights");
        }
    }
}
