using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class AddAddressPayoutRelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_known_address",
                table: "known_address");

            migrationBuilder.DropColumn(
                name: "id",
                table: "known_address");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "known_address",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "known_address",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_for",
                table: "known_address",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_known_address",
                table: "known_address",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "ix_known_address_payout_for",
                table: "known_address",
                column: "payout_for");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_known_address",
                table: "known_address");

            migrationBuilder.DropIndex(
                name: "ix_known_address_payout_for",
                table: "known_address");

            migrationBuilder.DropColumn(
                name: "payout_for",
                table: "known_address");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "known_address",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "known_address",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "known_address",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_known_address",
                table: "known_address",
                column: "id");
        }
    }
}
