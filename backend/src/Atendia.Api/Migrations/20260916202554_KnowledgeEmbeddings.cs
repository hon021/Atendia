using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Atendia.Api.Migrations
{
    /// <inheritdoc />
    public partial class KnowledgeEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "KnowledgeItems",
                type: "vector(1536)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_TenantId_BotId",
                table: "KnowledgeItems",
                columns: new[] { "TenantId", "BotId" });

            migrationBuilder.Sql("CREATE INDEX \"IX_KnowledgeItems_Embedding_Hnsw\" ON \"KnowledgeItems\" USING hnsw (\"Embedding\" vector_cosine_ops) WHERE \"Embedding\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_TenantId_BotId",
                table: "KnowledgeItems");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_KnowledgeItems_Embedding_Hnsw\";");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "KnowledgeItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
