using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class RemoveDelegateRewardsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delegate_rewards");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "delegate_rewards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accured = table.Column<long>(type: "bigint", nullable: false),
                    cycle = table.Column<int>(type: "integer", nullable: false),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    delivered = table.Column<long>(type: "bigint", nullable: false),
                    rewards = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegate_rewards", x => x.id);
                    table.ForeignKey(
                        name: "fk_delegate_rewards_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delegate_rewards_delegate_id",
                table: "delegate_rewards",
                column: "delegate_id");
        }
    }
}
