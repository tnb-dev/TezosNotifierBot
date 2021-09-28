using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class UpgradeUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance_update");

            migrationBuilder.DropTable(
                name: "user_address_delegation");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "whale_transaction_notify",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "user_address",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "user",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "message",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                table: "user");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "whale_transaction_notify",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "user_address",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "user",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "message",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "balance_update",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    slots = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balance_update", x => x.id);
                    table.ForeignKey(
                        name: "fk_balance_update_delegates_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_address_delegation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    user_address_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_address_delegation", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_address_delegation_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_address_delegation_user_address_user_address_id",
                        column: x => x.user_address_id,
                        principalTable: "user_address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_balance_update_delegate_id",
                table: "balance_update",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_address_delegation_delegate_id",
                table: "user_address_delegation",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_address_delegation_user_address_id",
                table: "user_address_delegation",
                column: "user_address_id");
        }
    }
}
