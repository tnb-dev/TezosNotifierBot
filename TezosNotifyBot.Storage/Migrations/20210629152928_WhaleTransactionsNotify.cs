using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class WhaleTransactionsNotify : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "whale_transaction_notify",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    whale_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whale_transaction_notify", x => x.id);
                    table.ForeignKey(
                        name: "fk_whale_transaction_notify_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_whale_transaction_notify_whale_transaction_whale_transactio~",
                        column: x => x.whale_transaction_id,
                        principalTable: "whale_transaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_whale_transaction_notify_user_id",
                table: "whale_transaction_notify",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_whale_transaction_notify_whale_transaction_id",
                table: "whale_transaction_notify",
                column: "whale_transaction_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whale_transaction_notify");
        }
    }
}
