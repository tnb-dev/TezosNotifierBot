using Microsoft.EntityFrameworkCore.Migrations;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class DisableNetworkIssueNotify : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"user\" SET network_issue_notify = 0 WHERE true;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
