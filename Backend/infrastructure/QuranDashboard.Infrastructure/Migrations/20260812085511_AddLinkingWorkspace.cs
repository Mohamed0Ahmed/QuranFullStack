using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkingWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "linking_workspaces",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_sources",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    source_identity = table.Column<string>(type: "text", nullable: false),
                    source_identity_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "jsonb", nullable: false),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    lemma_id = table.Column<int>(type: "integer", nullable: true),
                    stem_id = table.Column<int>(type: "integer", nullable: true),
                    unique_simple_word_id = table.Column<int>(type: "integer", nullable: true),
                    unique_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    word_type_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    inclusion_mode = table.Column<string>(type: "text", nullable: false),
                    automatic_word_matches_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    manual_link_shape = table.Column<string>(type: "text", nullable: true),
                    last_resolved_count = table.Column<int>(type: "integer", nullable: true),
                    last_resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_sources", x => x.id);
                    table.CheckConstraint("ck_linking_workspace_sources_inclusion_mode", "inclusion_mode IN ('all_except', 'only')");
                    table.CheckConstraint("ck_linking_workspace_sources_kind_configuration_coherence", "(source_kind = 'manual_mushaf_ayahs'\n    AND automatic_word_matches_enabled IS NULL\n    AND manual_link_shape IS NOT NULL)\nOR (source_kind <> 'manual_mushaf_ayahs'\n    AND automatic_word_matches_enabled IS NOT NULL\n    AND manual_link_shape IS NULL)");
                    table.CheckConstraint("ck_linking_workspace_sources_kind_reference_coherence", "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");
                    table.CheckConstraint("ck_linking_workspace_sources_manual_link_shape", "manual_link_shape IS NULL OR manual_link_shape IN ('grouped', 'independent')");
                    table.CheckConstraint("ck_linking_workspace_sources_scope_schema_version", "jsonb_typeof(scope) = 'object'\nAND jsonb_exists(scope, 'schemaVersion')\nAND jsonb_typeof(scope -> 'schemaVersion') = 'number'\nAND (scope ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (scope ->> 'schemaVersion')::numeric <= 2147483647");
                    table.CheckConstraint("ck_linking_workspace_sources_source_kind", "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_linking_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "linking_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_words_unique_simple_unique_~",
                        column: x => x.unique_simple_word_id,
                        principalTable: "quran_words_unique_simple",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_words_unique_tashkeel_uniqu~",
                        column: x => x.unique_tashkeel_word_id,
                        principalTable: "quran_words_unique_tashkeel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_words_unique_tashkeel_word_~",
                        column: x => x.word_type_tashkeel_word_id,
                        principalTable: "quran_words_unique_tashkeel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_ayah_overrides",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_ayah_overrides", x => new { x.workspace_source_id, x.ayah_id });
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_ayah_overrides_linking_workspace_s~",
                        column: x => x.workspace_source_id,
                        principalTable: "linking_workspace_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_ayah_overrides_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_manual_ayahs",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    page_hint = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_manual_ayahs", x => new { x.workspace_source_id, x.ayah_id });
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_manual_ayahs_linking_workspace_sou~",
                        column: x => x.workspace_source_id,
                        principalTable: "linking_workspace_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_manual_ayahs_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_words",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_words", x => new { x.workspace_source_id, x.quran_word_id });
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_words_linking_workspace_sources_wo~",
                        column: x => x.workspace_source_id,
                        principalTable: "linking_workspace_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_words_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_words_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_ayah_overrides_ayah_id",
                table: "linking_workspace_source_ayah_overrides",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_manual_ayahs_ayah_id",
                table: "linking_workspace_source_manual_ayahs",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_manual_ayahs_workspace_source_id_o~",
                table: "linking_workspace_source_manual_ayahs",
                columns: new[] { "workspace_source_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_ayah_id",
                table: "linking_workspace_source_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_quran_word_id",
                table: "linking_workspace_source_words",
                column: "quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_workspace_source_id_ayah_id",
                table: "linking_workspace_source_words",
                columns: new[] { "workspace_source_id", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_created_by",
                table: "linking_workspace_sources",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_lemma_id",
                table: "linking_workspace_sources",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_root_id",
                table: "linking_workspace_sources",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_stem_id",
                table: "linking_workspace_sources",
                column: "stem_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_unique_simple_word_id",
                table: "linking_workspace_sources",
                column: "unique_simple_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_unique_tashkeel_word_id",
                table: "linking_workspace_sources",
                column: "unique_tashkeel_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_updated_by",
                table: "linking_workspace_sources",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_word_type_tashkeel_word_id",
                table: "linking_workspace_sources",
                column: "word_type_tashkeel_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_workspace_id_order_value",
                table: "linking_workspace_sources",
                columns: new[] { "workspace_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_workspace_id_source_identity_hash",
                table: "linking_workspace_sources",
                columns: new[] { "workspace_id", "source_identity_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_created_by",
                table: "linking_workspaces",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_updated_by",
                table: "linking_workspaces",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_user_id",
                table: "linking_workspaces",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linking_workspace_source_ayah_overrides");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_manual_ayahs");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_words");

            migrationBuilder.DropTable(
                name: "linking_workspace_sources");

            migrationBuilder.DropTable(
                name: "linking_workspaces");
        }
    }
}
