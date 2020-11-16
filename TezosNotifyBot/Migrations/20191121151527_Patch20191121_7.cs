using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191121_7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EndorsingRights",
                columns: table => new
                {
                    EndorsingRightsID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DelegateId = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    SlotCount = table.Column<int>(nullable: false)
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
                name: "IX_EndorsingRights_DelegateId",
                table: "EndorsingRights",
                column: "DelegateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EndorsingRights");
        }
    }
}
