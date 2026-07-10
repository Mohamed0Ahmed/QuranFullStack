using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddUniqueSimpleImlaeiIdentity : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_simple_text_uthmani_simple",
                table: "quran_words_unique_simple");

            migrationBuilder.AddColumn<string>(
                name: "qpc_glyph",
                table: "quran_words_unique_simple",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "text_uthmani",
                table: "quran_words_unique_simple",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "word_key_imlaei_simple",
                table: "quran_words_unique_simple",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "word_key_imlaei_simple",
                table: "quran_words_ordered_simple",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_word_key_imlaei_simple",
                table: "quran_words_unique_simple",
                column: "word_key_imlaei_simple",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quran_words_unique_simple_word_key_imlaei_simple",
                table: "quran_words_unique_simple");

            migrationBuilder.DropColumn(
                name: "qpc_glyph",
                table: "quran_words_unique_simple");

            migrationBuilder.DropColumn(
                name: "text_uthmani",
                table: "quran_words_unique_simple");

            migrationBuilder.DropColumn(
                name: "word_key_imlaei_simple",
                table: "quran_words_unique_simple");

            migrationBuilder.DropColumn(
                name: "word_key_imlaei_simple",
                table: "quran_words_ordered_simple");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_text_uthmani_simple",
                table: "quran_words_unique_simple",
                column: "text_uthmani_simple",
                unique: true);
        }
    }
}
