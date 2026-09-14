using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atendia.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConversationUsagePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "UsageRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_TenantId_TimestampUtc",
                table: "UsageRecords",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_ConversationId_CreatedAtUtc",
                table: "Messages",
                columns: new[] { "TenantId", "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_CreatedAtUtc",
                table: "Leads",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_BotId_CreatedAtUtc",
                table: "Conversations",
                columns: new[] { "TenantId", "BotId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageRecords_TenantId_TimestampUtc",
                table: "UsageRecords");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId_ConversationId_CreatedAtUtc",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_CreatedAtUtc",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_BotId_CreatedAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "UsageRecords");
        }
    }
}
