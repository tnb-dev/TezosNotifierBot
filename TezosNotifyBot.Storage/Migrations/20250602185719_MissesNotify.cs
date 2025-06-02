using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TezosNotifyBot.Storage.Migrations
{
    /// <inheritdoc />
    public partial class MissesNotify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "down_end",
                table: "user_address",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "down_message_id",
                table: "user_address",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "down_start",
                table: "user_address",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "down_end",
                table: "user_address");

            migrationBuilder.DropColumn(
                name: "down_message_id",
                table: "user_address");

            migrationBuilder.DropColumn(
                name: "down_start",
                table: "user_address");
        }
    }
}
