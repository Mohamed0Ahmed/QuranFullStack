using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranWordIdentityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "unique_simple_word_id",
                table: "quran_words",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unique_tashkeel_word_id",
                table: "quran_words",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_word_id",
                table: "quran_words",
                column: "unique_simple_word_id",
                filter: "is_ayah_marker = false AND unique_simple_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_word_id",
                table: "quran_words",
                column: "unique_tashkeel_word_id",
                filter: "is_ayah_marker = false AND unique_tashkeel_word_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_simple_word_id",
                table: "quran_words");

            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_tashkeel_word_id",
                table: "quran_words");

            migrationBuilder.DropColumn(
                name: "unique_simple_word_id",
                table: "quran_words");

            migrationBuilder.DropColumn(
                name: "unique_tashkeel_word_id",
                table: "quran_words");
        }
    }
}
