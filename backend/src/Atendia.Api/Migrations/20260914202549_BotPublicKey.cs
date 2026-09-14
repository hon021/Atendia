using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atendia.Api.Migrations
{
    /// <inheritdoc />
    public partial class BotPublicKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "Bots",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Bots\" SET \"PublicKey\" = 'atd_' || md5(random()::text || clock_timestamp()::text) WHERE \"PublicKey\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "PublicKey",
                table: "Bots",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bots_PublicKey",
                table: "Bots",
                column: "PublicKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bots_PublicKey",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Bots");
        }
    }
}
