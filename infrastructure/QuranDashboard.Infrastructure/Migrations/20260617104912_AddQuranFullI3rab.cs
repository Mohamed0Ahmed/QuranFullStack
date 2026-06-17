using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranFullI3rab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    short_name_ar = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    short_name_en = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    contributor_name_ar = table.Column<string>(type: "text", nullable: true),
                    contributor_name_en = table.Column<string>(type: "text", nullable: true),
                    resource_kind = table.Column<string>(type: "text", nullable: false),
                    markup_format = table.Column<string>(type: "text", nullable: false),
                    has_quran_quotation_markup = table.Column<bool>(type: "boolean", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false),
                    package_file = table.Column<string>(type: "text", nullable: false),
                    source_file_original = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    license_status = table.Column<string>(type: "text", nullable: false),
                    provenance_status = table.Column<string>(type: "text", nullable: false),
                    usage_scope = table.Column<string>(type: "text", nullable: false),
                    manifest_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_sources", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_language_code", "language_code = 'ar'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_license_status", "license_status = 'unknown'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_markup_format", "markup_format = 'html'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_provenance_status", "provenance_status = 'unknown'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_resource_kind", "resource_kind = 'full_i3rab'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_usage_scope", "usage_scope = 'internal-only-until-cleared'");
                });

            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    source_entry_key = table.Column<string>(type: "text", nullable: false),
                    leader_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    i3rab_html = table.Column<string>(type: "text", nullable: false),
                    covered_ayah_count = table.Column<short>(type: "smallint", nullable: false),
                    covered_ayah_keys = table.Column<string>(type: "jsonb", nullable: false),
                    source_shape = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_entries", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_entries_covered_ayah_count", "covered_ayah_count >= 1");
                    table.CheckConstraint("CK_quran_full_i3rab_entries_i3rab_html", "i3rab_html <> ''");
                    table.CheckConstraint("CK_quran_full_i3rab_entries_source_shape", "source_shape IN ('grouped_leader', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_entries_quran_ayahs_leader_ayah_id",
                        column: x => x.leader_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_entries_quran_full_i3rab_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_full_i3rab_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<long>(type: "bigint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    source_value_kind = table.Column<string>(type: "text", nullable: false),
                    source_leader_verse_key = table.Column<string>(type: "text", nullable: false),
                    is_group_leader = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_ayah_entries_source_value_kind", "source_value_kind IN ('leader', 'member_pointer', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_entries_entr~",
                        column: x => x.entry_id,
                        principalTable: "quran_full_i3rab_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_sources_sour~",
                        column: x => x.source_id,
                        principalTable: "quran_full_i3rab_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_ayah_id_source_id",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_entry_id",
                table: "quran_full_i3rab_ayah_entries",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_source_id_ayah_id",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_source_id_verse_key",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "source_id", "verse_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_leader_ayah_id",
                table: "quran_full_i3rab_entries",
                column: "leader_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_source_id_leader_ayah_id",
                table: "quran_full_i3rab_entries",
                columns: new[] { "source_id", "leader_ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_source_id_source_entry_key",
                table: "quran_full_i3rab_entries",
                columns: new[] { "source_id", "source_entry_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_sources_package_file",
                table: "quran_full_i3rab_sources",
                column: "package_file",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_sources_source_key",
                table: "quran_full_i3rab_sources",
                column: "source_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_full_i3rab_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_full_i3rab_entries");

            migrationBuilder.DropTable(
                name: "quran_full_i3rab_sources");
        }
    }
}
