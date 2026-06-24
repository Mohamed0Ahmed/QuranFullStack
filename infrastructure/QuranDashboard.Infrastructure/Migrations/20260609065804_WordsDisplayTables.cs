using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class WordsDisplayTables : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_words_ordered_simple",
                columns: table => new
                {
                    word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_ayah = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_surah = table.Column<short>(type: "smallint", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_ordered_simple", x => x.word_order_in_mushaf);
                    table.ForeignKey(
                        name: "FK_quran_words_ordered_simple_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_ordered_tashkeel",
                columns: table => new
                {
                    word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_ayah = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_surah = table.Column<short>(type: "smallint", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_ordered_tashkeel", x => x.word_order_in_mushaf);
                    table.ForeignKey(
                        name: "FK_quran_words_ordered_tashkeel_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_unique_simple",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    first_location = table.Column<string>(type: "text", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    first_page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_line_number = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_unique_simple", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_unique_simple_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_unique_tashkeel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    first_location = table.Column<string>(type: "text", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    first_page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_line_number = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_unique_tashkeel", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_unique_tashkeel_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_quran_word_id",
                table: "quran_words_ordered_simple",
                column: "quran_word_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_surah_number_ayah_number_word_or~",
                table: "quran_words_ordered_simple",
                columns: new[] { "surah_number", "ayah_number", "word_order_in_ayah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_surah_number_word_order_in_surah",
                table: "quran_words_ordered_simple",
                columns: new[] { "surah_number", "word_order_in_surah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_quran_word_id",
                table: "quran_words_ordered_tashkeel",
                column: "quran_word_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_surah_number_ayah_number_word_~",
                table: "quran_words_ordered_tashkeel",
                columns: new[] { "surah_number", "ayah_number", "word_order_in_ayah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_surah_number_word_order_in_sur~",
                table: "quran_words_ordered_tashkeel",
                columns: new[] { "surah_number", "word_order_in_surah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_first_quran_word_id",
                table: "quran_words_unique_simple",
                column: "first_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_first_word_order_in_mushaf",
                table: "quran_words_unique_simple",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_text_uthmani_simple",
                table: "quran_words_unique_simple",
                column: "text_uthmani_simple",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_first_quran_word_id",
                table: "quran_words_unique_tashkeel",
                column: "first_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_first_word_order_in_mushaf",
                table: "quran_words_unique_tashkeel",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_text_uthmani",
                table: "quran_words_unique_tashkeel",
                column: "text_uthmani",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_words_ordered_simple");

            migrationBuilder.DropTable(
                name: "quran_words_ordered_tashkeel");

            migrationBuilder.DropTable(
                name: "quran_words_unique_simple");

            migrationBuilder.DropTable(
                name: "quran_words_unique_tashkeel");
        }
    }
}
