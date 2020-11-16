using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Migrations
{
    public partial class Patch0807 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "UserAddresses",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Delegates",
                columns: table => new
                {
                    DelegateId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegates", x => x.DelegateId);
                });

            migrationBuilder.CreateTable(
                name: "UserAddressDelegations",
                columns: table => new
                {
                    UserAddressDelegationId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserAddressId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddressDelegations", x => x.UserAddressDelegationId);
                    table.ForeignKey(
                        name: "FK_UserAddressDelegations_Delegates_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Delegates",
                        principalColumn: "DelegateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAddressDelegations_UserAddresses_UserAddressId",
                        column: x => x.UserAddressId,
                        principalTable: "UserAddresses",
                        principalColumn: "UserAddressId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAddressDelegations_DelegateId",
                table: "UserAddressDelegations",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddressDelegations_UserAddressId",
                table: "UserAddressDelegations",
                column: "UserAddressId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAddressDelegations");

            migrationBuilder.DropTable(
                name: "Delegates");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "UserAddresses");
        }
    }
}
