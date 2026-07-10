using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueWordSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "search_text_normalized",
                table: "quran_words_unique_tashkeel",
                type: "text",
                nullable: true,
                computedColumnSql: "translate(lower(text_uthmani_simple || ' ' || text_imlaei_simple), 'أإآٱؤئةىي', 'ااااواهيي')",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "search_text_normalized",
                table: "quran_words_unique_simple",
                type: "text",
                nullable: true,
                computedColumnSql: "translate(lower(text_uthmani_simple || ' ' || text_imlaei_simple || ' ' || word_key_imlaei_simple), 'أإآٱؤئةىي', 'ااااواهيي')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_search_text_normalized",
                table: "quran_words_unique_tashkeel",
                column: "search_text_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_search_text_normalized",
                table: "quran_words_unique_simple",
                column: "search_text_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_tashkeel_search_text_normalized",
                table: "quran_words_unique_tashkeel");

            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_simple_search_text_normalized",
                table: "quran_words_unique_simple");

            migrationBuilder.DropColumn(
                name: "search_text_normalized",
                table: "quran_words_unique_tashkeel");

            migrationBuilder.DropColumn(
                name: "search_text_normalized",
                table: "quran_words_unique_simple");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
