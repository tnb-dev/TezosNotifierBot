using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch20191121_4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalanceUpdates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BalanceUpdates",
                columns: table => new
                {
                    BalanceUpdateID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<long>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_BalanceUpdates_DelegateId",
                table: "BalanceUpdates",
                column: "DelegateId");
        }
    }
}
