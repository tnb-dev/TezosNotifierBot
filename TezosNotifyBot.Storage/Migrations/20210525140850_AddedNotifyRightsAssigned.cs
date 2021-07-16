using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class AddedNotifyRightsAssigned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "baking_rights");

            migrationBuilder.DropTable(
                name: "endorsing_rights");

            migrationBuilder.AddColumn<bool>(
                name: "notify_rights_assigned",
                table: "user_address",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notify_rights_assigned",
                table: "user_address");

            migrationBuilder.CreateTable(
                name: "baking_rights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_baking_rights", x => x.id);
                    table.ForeignKey(
                        name: "fk_baking_rights_delegates_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endorsing_rights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    slot_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_endorsing_rights", x => x.id);
                    table.ForeignKey(
                        name: "fk_endorsing_rights_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_baking_rights_delegate_id",
                table: "baking_rights",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_endorsing_rights_delegate_id",
                table: "endorsing_rights",
                column: "delegate_id");
        }
    }
}
