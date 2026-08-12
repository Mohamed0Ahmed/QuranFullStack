using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkingConfirmedState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "linking_operations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_count = table.Column<int>(type: "integer", nullable: false),
                    ayah_count = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_operations", x => x.id);
                    table.CheckConstraint("ck_linking_operations_outcome_schema_version", "jsonb_typeof(outcome) = 'object'\nAND jsonb_exists(outcome, 'schemaVersion')\nAND jsonb_typeof(outcome -> 'schemaVersion') = 'number'\nAND (outcome ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (outcome ->> 'schemaVersion')::numeric <= 2147483647");
                    table.ForeignKey(
                        name: "FK_linking_operations_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_operations_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_source_contributions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    operation_id = table.Column<long>(type: "bigint", nullable: false),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    contribution_mode = table.Column<string>(type: "text", nullable: false),
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
                    resolved_ayah_count = table.Column<int>(type: "integer", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_source_contributions", x => x.id);
                    table.UniqueConstraint("AK_linking_source_contributions_id_door_id", x => new { x.id, x.door_id });
                    table.CheckConstraint("ck_linking_source_contributions_contribution_mode", "contribution_mode IN ('automatic', 'manual_single', 'manual_independent', 'manual_grouped')");
                    table.CheckConstraint("ck_linking_source_contributions_kind_reference_coherence", "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");
                    table.CheckConstraint("ck_linking_source_contributions_manual_mode_coherence", "(source_kind = 'manual_mushaf_ayahs'\n    AND contribution_mode IN ('manual_single', 'manual_independent', 'manual_grouped'))\nOR (source_kind <> 'manual_mushaf_ayahs'\n    AND contribution_mode = 'automatic')");
                    table.CheckConstraint("ck_linking_source_contributions_scope_schema_version", "jsonb_typeof(scope) = 'object'\nAND jsonb_exists(scope, 'schemaVersion')\nAND jsonb_typeof(scope -> 'schemaVersion') = 'number'\nAND (scope ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (scope ->> 'schemaVersion')::numeric <= 2147483647");
                    table.CheckConstraint("ck_linking_source_contributions_source_kind", "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_linking_operations_operation_id",
                        column: x => x.operation_id,
                        principalTable: "linking_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_words_unique_simple_uniq~",
                        column: x => x.unique_simple_word_id,
                        principalTable: "quran_words_unique_simple",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_words_unique_tashkeel_un~",
                        column: x => x.unique_tashkeel_word_id,
                        principalTable: "quran_words_unique_tashkeel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_words_unique_tashkeel_wo~",
                        column: x => x.word_type_tashkeel_word_id,
                        principalTable: "quran_words_unique_tashkeel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_units",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_contribution_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    is_grouped = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_units", x => x.id);
                    table.UniqueConstraint("AK_linking_units_id_source_contribution_id", x => new { x.id, x.source_contribution_id });
                    table.ForeignKey(
                        name: "FK_linking_units_linking_source_contributions_source_contribut~",
                        column: x => x.source_contribution_id,
                        principalTable: "linking_source_contributions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayahs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    source_contribution_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayahs", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayahs_linking_units_unit_id_source_contributio~",
                        columns: x => new { x.unit_id, x.source_contribution_id },
                        principalTable: "linking_units",
                        principalColumns: new[] { "id", "source_contribution_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayahs_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayah_descriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayah_descriptions", x => x.id);
                    table.CheckConstraint("ck_linking_unit_ayah_descriptions_body_not_blank", "btrim(body) <> ''");
                    table.CheckConstraint("ck_linking_unit_ayah_descriptions_order_value", "order_value BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_descriptions_linking_unit_ayahs_unit_ayah~",
                        column: x => x.unit_ayah_id,
                        principalTable: "linking_unit_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_descriptions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_descriptions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayah_words",
                columns: table => new
                {
                    unit_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayah_words", x => new { x.unit_ayah_id, x.quran_word_id });
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_words_linking_unit_ayahs_unit_ayah_id",
                        column: x => x.unit_ayah_id,
                        principalTable: "linking_unit_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_words_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_words_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_actor_user_id",
                table: "linking_operations",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_door_id_confirmed_at",
                table: "linking_operations",
                columns: new[] { "door_id", "confirmed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_idempotency_key",
                table: "linking_operations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_created_by",
                table: "linking_source_contributions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_deleted_by",
                table: "linking_source_contributions",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_door_id",
                table: "linking_source_contributions",
                column: "door_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_door_id_source_identity_hash",
                table: "linking_source_contributions",
                columns: new[] { "door_id", "source_identity_hash" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_lemma_id",
                table: "linking_source_contributions",
                column: "lemma_id",
                filter: "lemma_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_operation_id_order_value",
                table: "linking_source_contributions",
                columns: new[] { "operation_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_root_id",
                table: "linking_source_contributions",
                column: "root_id",
                filter: "root_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_stem_id",
                table: "linking_source_contributions",
                column: "stem_id",
                filter: "stem_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_unique_simple_word_id",
                table: "linking_source_contributions",
                column: "unique_simple_word_id",
                filter: "unique_simple_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_unique_tashkeel_word_id",
                table: "linking_source_contributions",
                column: "unique_tashkeel_word_id",
                filter: "unique_tashkeel_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_updated_by",
                table: "linking_source_contributions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_word_type_tashkeel_word_id",
                table: "linking_source_contributions",
                column: "word_type_tashkeel_word_id",
                filter: "word_type_tashkeel_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_created_by",
                table: "linking_unit_ayah_descriptions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_unit_ayah_id_order_value",
                table: "linking_unit_ayah_descriptions",
                columns: new[] { "unit_ayah_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_updated_by",
                table: "linking_unit_ayah_descriptions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_words_ayah_id",
                table: "linking_unit_ayah_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_words_quran_word_id",
                table: "linking_unit_ayah_words",
                column: "quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_ayah_id",
                table: "linking_unit_ayahs",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_source_contribution_id_ayah_id",
                table: "linking_unit_ayahs",
                columns: new[] { "source_contribution_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_unit_id_order_value",
                table: "linking_unit_ayahs",
                columns: new[] { "unit_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_unit_id_source_contribution_id",
                table: "linking_unit_ayahs",
                columns: new[] { "unit_id", "source_contribution_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_units_source_contribution_id_order_value",
                table: "linking_units",
                columns: new[] { "source_contribution_id", "order_value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linking_unit_ayah_descriptions");

            migrationBuilder.DropTable(
                name: "linking_unit_ayah_words");

            migrationBuilder.DropTable(
                name: "linking_unit_ayahs");

            migrationBuilder.DropTable(
                name: "linking_units");

            migrationBuilder.DropTable(
                name: "linking_source_contributions");

            migrationBuilder.DropTable(
                name: "linking_operations");
        }
    }
}
