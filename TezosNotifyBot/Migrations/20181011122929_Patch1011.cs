using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch1011 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "LastBlock",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DelegateRewards",
                columns: table => new
                {
                    DelegateRewardsId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Rewards = table.Column<long>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Cycle = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegateRewards", x => x.DelegateRewardsId);
                    table.ForeignKey(
                        name: "FK_DelegateRewards_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DelegateRewards_DelegateId",
                table: "DelegateRewards",
                column: "DelegateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DelegateRewards");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "LastBlock");
        }
    }
}
