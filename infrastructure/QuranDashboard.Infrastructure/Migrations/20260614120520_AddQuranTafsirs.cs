using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddQuranTafsirs : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_tafsir_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    language_name_ar = table.Column<string>(type: "text", nullable: false),
                    language_name_en = table.Column<string>(type: "text", nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    short_name_ar = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    short_name_en = table.Column<string>(type: "text", nullable: false),
                    contributor_key = table.Column<string>(type: "text", nullable: true),
                    contributor_name_ar = table.Column<string>(type: "text", nullable: true),
                    contributor_name_en = table.Column<string>(type: "text", nullable: true),
                    contributor_type = table.Column<string>(type: "text", nullable: false),
                    resource_kind = table.Column<string>(type: "text", nullable: false),
                    tafsir_kind = table.Column<string>(type: "text", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false),
                    package_file = table.Column<string>(type: "text", nullable: false),
                    source_file_original = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    license_status = table.Column<string>(type: "text", nullable: false),
                    provenance_status = table.Column<string>(type: "text", nullable: false),
                    manifest_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_sources", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_tafsir_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_tafsir_sources_resource_kind", "resource_kind = 'tafsir'");
                });

            migrationBuilder.CreateTable(
                name: "quran_tafsir_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    source_entry_key = table.Column<string>(type: "text", nullable: false),
                    leader_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    tafsir_text = table.Column<string>(type: "text", nullable: false),
                    covered_ayah_count = table.Column<short>(type: "smallint", nullable: false),
                    covered_ayah_keys = table.Column<string>(type: "jsonb", nullable: false),
                    source_shape = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_entries", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_entries_covered_ayah_count", "covered_ayah_count >= 1");
                    table.CheckConstraint("CK_quran_tafsir_entries_source_shape", "source_shape IN ('grouped_leader', 'flat')");
                    table.CheckConstraint("CK_quran_tafsir_entries_tafsir_text", "tafsir_text <> ''");
                    table.ForeignKey(
                        name: "FK_quran_tafsir_entries_quran_ayahs_leader_ayah_id",
                        column: x => x.leader_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_entries_quran_tafsir_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_tafsir_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_tafsir_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    tafsir_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    source_value_kind = table.Column<string>(type: "text", nullable: false),
                    source_leader_verse_key = table.Column<string>(type: "text", nullable: false),
                    is_group_leader = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_ayah_entries_source_value_kind", "source_value_kind IN ('leader', 'member_pointer', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_tafsir_entries_tafsir_entry~",
                        column: x => x.tafsir_entry_id,
                        principalTable: "quran_tafsir_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_tafsir_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_tafsir_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_ayah_id_source_id",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_source_id_ayah_id",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_source_id_verse_key",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "source_id", "verse_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_tafsir_entry_id",
                table: "quran_tafsir_ayah_entries",
                column: "tafsir_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_leader_ayah_id",
                table: "quran_tafsir_entries",
                column: "leader_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_source_id_leader_ayah_id",
                table: "quran_tafsir_entries",
                columns: new[] { "source_id", "leader_ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_source_id_source_entry_key",
                table: "quran_tafsir_entries",
                columns: new[] { "source_id", "source_entry_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_language_code",
                table: "quran_tafsir_sources",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_language_code_tafsir_kind",
                table: "quran_tafsir_sources",
                columns: new[] { "language_code", "tafsir_kind" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_package_file",
                table: "quran_tafsir_sources",
                column: "package_file",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_source_key",
                table: "quran_tafsir_sources",
                column: "source_key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_tafsir_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_tafsir_entries");

            migrationBuilder.DropTable(
                name: "quran_tafsir_sources");
        }
    }
}
