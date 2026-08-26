using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranPhraseSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_phrase_index_builds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    format_version = table.Column<int>(type: "integer", nullable: false),
                    exact_ready = table.Column<bool>(type: "boolean", nullable: false),
                    similarity_ready = table.Column<bool>(type: "boolean", nullable: false),
                    builder_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_revision = table.Column<long>(type: "bigint", nullable: false),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    validated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    search_token_count = table.Column<long>(type: "bigint", nullable: false),
                    variant_count = table.Column<long>(type: "bigint", nullable: false),
                    occurrence_count = table.Column<long>(type: "bigint", nullable: false),
                    similarity_edge_count = table.Column<long>(type: "bigint", nullable: false),
                    similarity_anchor_stat_count = table.Column<long>(type: "bigint", nullable: false),
                    validation_verdict = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    report_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    failure_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_index_builds", x => x.id);
                    table.CheckConstraint("ck_quran_phrase_index_builds_active_readiness", "status <> 3 OR (exact_ready AND similarity_ready)");
                    table.CheckConstraint("ck_quran_phrase_index_builds_format_version", "format_version > 0");
                    table.CheckConstraint("ck_quran_phrase_index_builds_source_fingerprint", "source_fingerprint ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_quran_phrase_index_builds_source_revision", "source_revision > 0");
                    table.CheckConstraint("ck_quran_phrase_index_builds_status", "status IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("ck_quran_phrase_index_builds_totals", "search_token_count >= 0 AND variant_count >= 0 AND occurrence_count >= 0 AND similarity_edge_count >= 0 AND similarity_anchor_stat_count >= 0");
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_index_state",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    source_revision = table.Column<long>(type: "bigint", nullable: false),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    active_build_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_build_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false),
                    stale_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_index_state", x => x.id);
                    table.CheckConstraint("ck_quran_phrase_index_state_distinct_builds", "active_build_id IS NULL OR previous_build_id IS NULL OR active_build_id <> previous_build_id");
                    table.CheckConstraint("ck_quran_phrase_index_state_singleton", "id = 1");
                    table.CheckConstraint("ck_quran_phrase_index_state_source_fingerprint", "source_fingerprint IS NULL OR source_fingerprint ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_quran_phrase_index_state_source_revision", "source_revision >= 0");
                    table.CheckConstraint("ck_quran_phrase_index_state_stale_reason", "is_stale OR stale_reason IS NULL");
                    table.ForeignKey(
                        name: "FK_quran_phrase_index_state_quran_phrase_index_builds_active_b~",
                        column: x => x.active_build_id,
                        principalTable: "quran_phrase_index_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_quran_phrase_index_state_quran_phrase_index_builds_previous~",
                        column: x => x.previous_build_id,
                        principalTable: "quran_phrase_index_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_search_tokens",
                columns: table => new
                {
                    build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    search_text = table.Column<string>(type: "text", nullable: false),
                    exact_token_ids = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_search_tokens", x => new { x.build_id, x.mode, x.id });
                    table.CheckConstraint("ck_quran_phrase_search_tokens_exact_token_ids", "cardinality(exact_token_ids) > 0");
                    table.CheckConstraint("ck_quran_phrase_search_tokens_mode", "mode IN (1, 2)");
                    table.CheckConstraint("ck_quran_phrase_search_tokens_search_text", "btrim(search_text) <> ''");
                    table.ForeignKey(
                        name: "FK_quran_phrase_search_tokens_quran_phrase_index_builds_build_~",
                        column: x => x.build_id,
                        principalTable: "quran_phrase_index_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_variants",
                columns: table => new
                {
                    build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    word_count = table.Column<short>(type: "smallint", nullable: false),
                    exact_token_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    search_token_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    display_text = table.Column<string>(type: "text", nullable: false),
                    occurrence_count = table.Column<long>(type: "bigint", nullable: false),
                    ayah_count = table.Column<int>(type: "integer", nullable: false),
                    surah_count = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_variants", x => new { x.build_id, x.id });
                    table.UniqueConstraint("AK_quran_phrase_variants_build_id_id_mode_word_count", x => new { x.build_id, x.id, x.mode, x.word_count });
                    table.CheckConstraint("ck_quran_phrase_variants_counts", "occurrence_count > 0 AND ayah_count > 0 AND surah_count > 0 AND ayah_count <= occurrence_count AND surah_count <= ayah_count");
                    table.CheckConstraint("ck_quran_phrase_variants_display_text", "btrim(display_text) <> ''");
                    table.CheckConstraint("ck_quran_phrase_variants_exact_token_ids", "cardinality(exact_token_ids) = word_count");
                    table.CheckConstraint("ck_quran_phrase_variants_mode", "mode IN (1, 2)");
                    table.CheckConstraint("ck_quran_phrase_variants_search_token_ids", "cardinality(search_token_ids) = word_count");
                    table.CheckConstraint("ck_quran_phrase_variants_word_count", "word_count > 0");
                    table.ForeignKey(
                        name: "FK_quran_phrase_variants_quran_phrase_index_builds_build_id",
                        column: x => x.build_id,
                        principalTable: "quran_phrase_index_builds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_phrase_variants_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_occurrences",
                columns: table => new
                {
                    build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    variant_id = table.Column<long>(type: "bigint", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    word_count = table.Column<short>(type: "smallint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    start_word_number = table.Column<short>(type: "smallint", nullable: false),
                    end_word_number = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    last_quran_word_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_occurrences", x => new { x.build_id, x.id });
                    table.CheckConstraint("ck_quran_phrase_occurrences_mode", "mode IN (1, 2)");
                    table.CheckConstraint("ck_quran_phrase_occurrences_word_count", "word_count > 0");
                    table.CheckConstraint("ck_quran_phrase_occurrences_word_range", "start_word_number > 0 AND end_word_number - start_word_number + 1 = word_count");
                    table.ForeignKey(
                        name: "FK_quran_phrase_occurrences_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_phrase_occurrences_quran_phrase_variants_build_id_var~",
                        columns: x => new { x.build_id, x.variant_id, x.mode, x.word_count },
                        principalTable: "quran_phrase_variants",
                        principalColumns: new[] { "build_id", "id", "mode", "word_count" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_phrase_occurrences_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_phrase_occurrences_quran_words_last_quran_word_id",
                        column: x => x.last_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_similarity_anchor_stats",
                columns: table => new
                {
                    build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<long>(type: "bigint", nullable: false),
                    threshold = table.Column<short>(type: "smallint", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    word_count = table.Column<short>(type: "smallint", nullable: false),
                    neighbor_count = table.Column<int>(type: "integer", nullable: false),
                    best_matched_count = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_similarity_anchor_stats", x => new { x.build_id, x.variant_id, x.threshold });
                    table.CheckConstraint("ck_quran_phrase_similarity_anchor_stats_counts", "neighbor_count >= 0 AND (best_matched_count IS NULL OR (best_matched_count >= 0 AND best_matched_count <= word_count))");
                    table.CheckConstraint("ck_quran_phrase_similarity_anchor_stats_mode", "mode IN (1, 2)");
                    table.CheckConstraint("ck_quran_phrase_similarity_anchor_stats_threshold", "threshold IN (50, 60, 70, 80, 90)");
                    table.CheckConstraint("ck_quran_phrase_similarity_anchor_stats_word_count", "word_count >= 4");
                    table.ForeignKey(
                        name: "FK_quran_phrase_similarity_anchor_stats_quran_phrase_variants_~",
                        columns: x => new { x.build_id, x.variant_id, x.mode, x.word_count },
                        principalTable: "quran_phrase_variants",
                        principalColumns: new[] { "build_id", "id", "mode", "word_count" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_phrase_similarity_edges",
                columns: table => new
                {
                    build_id = table.Column<Guid>(type: "uuid", nullable: false),
                    left_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    right_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    word_count = table.Column<short>(type: "smallint", nullable: false),
                    matched_count = table.Column<short>(type: "smallint", nullable: false),
                    difference_count = table.Column<short>(type: "smallint", nullable: false),
                    difference_positions = table.Column<short[]>(type: "smallint[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_phrase_similarity_edges", x => new { x.build_id, x.left_variant_id, x.right_variant_id });
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_counts", "matched_count > 0 AND difference_count > 0 AND matched_count + difference_count = word_count");
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_difference_positions", "cardinality(difference_positions) = difference_count AND 0 < ALL (difference_positions) AND word_count >= ALL (difference_positions)");
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_minimum_match", "matched_count * 2 >= word_count");
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_mode", "mode IN (1, 2)");
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_order", "left_variant_id < right_variant_id");
                    table.CheckConstraint("ck_quran_phrase_similarity_edges_word_count", "word_count >= 4");
                    table.ForeignKey(
                        name: "FK_quran_phrase_similarity_edges_quran_phrase_variants_build_i~",
                        columns: x => new { x.build_id, x.left_variant_id, x.mode, x.word_count },
                        principalTable: "quran_phrase_variants",
                        principalColumns: new[] { "build_id", "id", "mode", "word_count" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_phrase_similarity_edges_quran_phrase_variants_build_~1",
                        columns: x => new { x.build_id, x.right_variant_id, x.mode, x.word_count },
                        principalTable: "quran_phrase_variants",
                        principalColumns: new[] { "build_id", "id", "mode", "word_count" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "quran_phrase_index_state",
                columns: new[] { "id", "active_build_id", "is_stale", "previous_build_id", "source_fingerprint", "source_revision", "stale_reason", "updated_at_utc" },
                values: new object[] { (short)1, null, false, null, null, 0L, null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_readable_ayah_word",
                table: "quran_words",
                columns: new[] { "ayah_id", "word_number" },
                filter: "is_ayah_marker = false");

            migrationBuilder.CreateIndex(
                name: "ux_quran_phrase_index_builds_active",
                table: "quran_phrase_index_builds",
                column: "status",
                unique: true,
                filter: "status = 3");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_index_state_active_build_id",
                table: "quran_phrase_index_state",
                column: "active_build_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_index_state_previous_build_id",
                table: "quran_phrase_index_state",
                column: "previous_build_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_ayah_id",
                table: "quran_phrase_occurrences",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_build_id_ayah_id_start_word_number~",
                table: "quran_phrase_occurrences",
                columns: new[] { "build_id", "ayah_id", "start_word_number", "end_word_number" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_build_id_variant_id_ayah_id_start_~",
                table: "quran_phrase_occurrences",
                columns: new[] { "build_id", "variant_id", "ayah_id", "start_word_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_build_id_variant_id_mode_word_count",
                table: "quran_phrase_occurrences",
                columns: new[] { "build_id", "variant_id", "mode", "word_count" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_first_quran_word_id",
                table: "quran_phrase_occurrences",
                column: "first_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_occurrences_last_quran_word_id",
                table: "quran_phrase_occurrences",
                column: "last_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_search_tokens_build_id_mode_search_text",
                table: "quran_phrase_search_tokens",
                columns: new[] { "build_id", "mode", "search_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_anchor_stats_build_id_mode_word_cou~",
                table: "quran_phrase_similarity_anchor_stats",
                columns: new[] { "build_id", "mode", "word_count", "threshold", "neighbor_count", "variant_id" },
                descending: new[] { false, false, false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_anchor_stats_build_id_variant_id_mo~",
                table: "quran_phrase_similarity_anchor_stats",
                columns: new[] { "build_id", "variant_id", "mode", "word_count" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_edges_build_id_left_variant_id_matc~",
                table: "quran_phrase_similarity_edges",
                columns: new[] { "build_id", "left_variant_id", "matched_count", "right_variant_id" },
                descending: new[] { false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_edges_build_id_left_variant_id_mode~",
                table: "quran_phrase_similarity_edges",
                columns: new[] { "build_id", "left_variant_id", "mode", "word_count" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_edges_build_id_right_variant_id_mat~",
                table: "quran_phrase_similarity_edges",
                columns: new[] { "build_id", "right_variant_id", "matched_count", "left_variant_id" },
                descending: new[] { false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_similarity_edges_build_id_right_variant_id_mod~",
                table: "quran_phrase_similarity_edges",
                columns: new[] { "build_id", "right_variant_id", "mode", "word_count" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_variants_build_id_mode_word_count_exact_token_~",
                table: "quran_phrase_variants",
                columns: new[] { "build_id", "mode", "word_count", "exact_token_ids" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_variants_build_id_mode_word_count_occurrence_c~",
                table: "quran_phrase_variants",
                columns: new[] { "build_id", "mode", "word_count", "occurrence_count", "id" },
                descending: new[] { false, false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_variants_build_id_mode_word_count_search_token~",
                table: "quran_phrase_variants",
                columns: new[] { "build_id", "mode", "word_count", "search_token_ids" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_phrase_variants_first_quran_word_id",
                table: "quran_phrase_variants",
                column: "first_quran_word_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_phrase_index_state");

            migrationBuilder.DropTable(
                name: "quran_phrase_occurrences");

            migrationBuilder.DropTable(
                name: "quran_phrase_search_tokens");

            migrationBuilder.DropTable(
                name: "quran_phrase_similarity_anchor_stats");

            migrationBuilder.DropTable(
                name: "quran_phrase_similarity_edges");

            migrationBuilder.DropTable(
                name: "quran_phrase_variants");

            migrationBuilder.DropTable(
                name: "quran_phrase_index_builds");

            migrationBuilder.DropIndex(
                name: "IX_quran_words_readable_ayah_word",
                table: "quran_words");
        }
    }
}
