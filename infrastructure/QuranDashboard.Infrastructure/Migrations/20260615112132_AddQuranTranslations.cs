using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_translation_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    language_name_en = table.Column<string>(type: "text", nullable: false),
                    language_name_ar = table.Column<string>(type: "text", nullable: false),
                    native_name = table.Column<string>(type: "text", nullable: true),
                    direction = table.Column<string>(type: "text", nullable: false),
                    translation_type = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    translator_key = table.Column<string>(type: "text", nullable: true),
                    translator_name_en = table.Column<string>(type: "text", nullable: true),
                    translator_name_ar = table.Column<string>(type: "text", nullable: true),
                    contains_inline_footnotes = table.Column<bool>(type: "boolean", nullable: false),
                    contains_html_markup = table.Column<bool>(type: "boolean", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_translation_sources", x => x.id);
                    table.CheckConstraint("CK_quran_translation_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_translation_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_translation_sources_required_fields", "btrim(source_key) <> '' AND\nbtrim(language_code) <> '' AND\nbtrim(language_name_en) <> '' AND\nbtrim(language_name_ar) <> '' AND\nbtrim(direction) <> '' AND\nbtrim(translation_type) <> '' AND\nbtrim(display_name_en) <> '' AND\nbtrim(display_name_ar) <> ''");
                    table.CheckConstraint("CK_quran_translation_sources_translation_type", "translation_type IN ('simple', 'with_footnotes')");
                });

            migrationBuilder.CreateTable(
                name: "quran_translation_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_translation_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_translation_ayah_entries_text", "text <> ''");
                    table.ForeignKey(
                        name: "FK_quran_translation_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_translation_ayah_entries_quran_translation_sources_so~",
                        column: x => x.source_id,
                        principalTable: "quran_translation_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_ayah_entries_ayah_id_source_id",
                table: "quran_translation_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_ayah_entries_source_id_ayah_id",
                table: "quran_translation_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_language_code",
                table: "quran_translation_sources",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_language_code_translation_type",
                table: "quran_translation_sources",
                columns: new[] { "language_code", "translation_type" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_source_key",
                table: "quran_translation_sources",
                column: "source_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_translation_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_translation_sources");
        }
    }
}
