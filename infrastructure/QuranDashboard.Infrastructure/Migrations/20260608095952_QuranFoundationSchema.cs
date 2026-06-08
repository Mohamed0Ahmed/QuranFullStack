using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuranFoundationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_mushaf_pages",
                columns: table => new
                {
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    last_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    last_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    lines_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mushaf_pages", x => x.page_number);
                });

            migrationBuilder.CreateTable(
                name: "quran_surahs",
                columns: table => new
                {
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    name_arabic = table.Column<string>(type: "text", nullable: false),
                    name_simple = table.Column<string>(type: "text", nullable: false),
                    name_transliteration = table.Column<string>(type: "text", nullable: false),
                    revelation_place = table.Column<string>(type: "text", nullable: false),
                    revelation_order = table.Column<short>(type: "smallint", nullable: false),
                    verses_count = table.Column<short>(type: "smallint", nullable: false),
                    bismillah_pre = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_surahs", x => x.surah_number);
                });

            migrationBuilder.CreateTable(
                name: "quran_ayahs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    words_count_source = table.Column<short>(type: "smallint", nullable: false),
                    words_count_real = table.Column<short>(type: "smallint", nullable: false),
                    page_from = table.Column<short>(type: "smallint", nullable: false),
                    page_to = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_ayahs", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_ayahs_quran_surahs_surah_number",
                        column: x => x.surah_number,
                        principalTable: "quran_surahs",
                        principalColumn: "surah_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    word_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    line_word_order = table.Column<short>(type: "smallint", nullable: false),
                    qpc_glyph = table.Column<string>(type: "text", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    is_ayah_marker = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_words_quran_mushaf_pages_page_number",
                        column: x => x.page_number,
                        principalTable: "quran_mushaf_pages",
                        principalColumn: "page_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_mushaf_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    line_type = table.Column<string>(type: "text", nullable: false),
                    is_centered = table.Column<bool>(type: "boolean", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: true),
                    first_word_id = table.Column<int>(type: "integer", nullable: true),
                    last_word_id = table.Column<int>(type: "integer", nullable: true),
                    words_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mushaf_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_mushaf_pages_page_number",
                        column: x => x.page_number,
                        principalTable: "quran_mushaf_pages",
                        principalColumn: "page_number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_surahs_surah_number",
                        column: x => x.surah_number,
                        principalTable: "quran_surahs",
                        principalColumn: "surah_number");
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_words_first_word_id",
                        column: x => x.first_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_words_last_word_id",
                        column: x => x.last_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_surah_number_ayah_number",
                table: "quran_ayahs",
                columns: new[] { "surah_number", "ayah_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_verse_key",
                table: "quran_ayahs",
                column: "verse_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_first_word_id",
                table: "quran_mushaf_lines",
                column: "first_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_last_word_id",
                table: "quran_mushaf_lines",
                column: "last_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_page_number_line_number",
                table: "quran_mushaf_lines",
                columns: new[] { "page_number", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_surah_number",
                table: "quran_mushaf_lines",
                column: "surah_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_surahs_name_arabic",
                table: "quran_surahs",
                column: "name_arabic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ayah_id",
                table: "quran_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_location",
                table: "quran_words",
                column: "location",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_page_number_line_number_line_word_order",
                table: "quran_words",
                columns: new[] { "page_number", "line_number", "line_word_order" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_readable_surah_ayah_word",
                table: "quran_words",
                columns: new[] { "surah_number", "ayah_number", "word_number" },
                filter: "is_ayah_marker = false");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_surah_ayah_word",
                table: "quran_words",
                columns: new[] { "surah_number", "ayah_number", "word_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_mushaf_lines");

            migrationBuilder.DropTable(
                name: "quran_words");

            migrationBuilder.DropTable(
                name: "quran_ayahs");

            migrationBuilder.DropTable(
                name: "quran_mushaf_pages");

            migrationBuilder.DropTable(
                name: "quran_surahs");
        }
    }
}
