using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddWordKeyImlaeiSimple : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "word_key_imlaei_simple",
                table: "quran_words",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_word_key_imlaei_simple",
                table: "quran_words",
                column: "word_key_imlaei_simple",
                filter: "is_ayah_marker = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quran_words_word_key_imlaei_simple",
                table: "quran_words");

            migrationBuilder.DropColumn(
                name: "word_key_imlaei_simple",
                table: "quran_words");
        }
    }
}
