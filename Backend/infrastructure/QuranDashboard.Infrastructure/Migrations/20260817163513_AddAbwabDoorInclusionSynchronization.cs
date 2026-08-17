using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabDoorInclusionSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_source_contributions_kind_reference_coherence",
                table: "linking_source_contributions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_source_contributions_source_kind",
                table: "linking_source_contributions");

            migrationBuilder.AlterColumn<long>(
                name: "operation_id",
                table: "linking_source_contributions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "door_inclusion_id",
                table: "linking_source_contributions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "abwab_door_inclusions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    target_door_id = table.Column<int>(type: "integer", nullable: false),
                    source_door_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_door_inclusions", x => x.id);
                    table.CheckConstraint("ck_abwab_door_inclusions_distinct_doors", "target_door_id <> source_door_id");
                    table.ForeignKey(
                        name: "FK_abwab_door_inclusions_abwab_doors_source_door_id",
                        column: x => x.source_door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_door_inclusions_abwab_doors_target_door_id",
                        column: x => x.target_door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abwab_door_inclusion_unit_syncs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_inclusion_id = table.Column<int>(type: "integer", nullable: false),
                    source_unit_id = table.Column<long>(type: "bigint", nullable: false),
                    target_unit_id = table.Column<long>(type: "bigint", nullable: true),
                    state = table.Column<string>(type: "text", nullable: false),
                    source_fingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_door_inclusion_unit_syncs", x => x.id);
                    table.CheckConstraint("ck_abwab_door_inclusion_unit_syncs_state", "state IN ('active', 'overridden', 'suppressed')");
                    table.CheckConstraint("ck_abwab_door_inclusion_unit_syncs_target_coherence", "(state IN ('active', 'overridden') AND target_unit_id IS NOT NULL) OR (state = 'suppressed' AND target_unit_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_abwab_door_inclusion_syncs_inclusion",
                        column: x => x.door_inclusion_id,
                        principalTable: "abwab_door_inclusions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_door_inclusion_syncs_source_unit",
                        column: x => x.source_unit_id,
                        principalTable: "linking_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_door_inclusion_syncs_target_unit",
                        column: x => x.target_unit_id,
                        principalTable: "linking_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_door_inclusion_id",
                table: "linking_source_contributions",
                column: "door_inclusion_id",
                unique: true,
                filter: "door_inclusion_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_source_contributions_kind_reference_coherence",
                table: "linking_source_contributions",
                sql: "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND door_inclusion_id IS NULL\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'door_inclusion'\n    AND door_inclusion_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_source_contributions_operation_ownership_coherence",
                table: "linking_source_contributions",
                sql: "(source_kind = 'door_inclusion' AND operation_id IS NULL AND door_inclusion_id IS NOT NULL)\nOR (source_kind <> 'door_inclusion' AND operation_id IS NOT NULL AND door_inclusion_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_source_contributions_source_kind",
                table: "linking_source_contributions",
                sql: "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs', 'door_inclusion')");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusion_syncs_inclusion_source",
                table: "abwab_door_inclusion_unit_syncs",
                columns: new[] { "door_inclusion_id", "source_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusion_syncs_source_unit",
                table: "abwab_door_inclusion_unit_syncs",
                column: "source_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusion_syncs_target_unit",
                table: "abwab_door_inclusion_unit_syncs",
                column: "target_unit_id",
                unique: true,
                filter: "target_unit_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusions_deleted_at",
                table: "abwab_door_inclusions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusions_source_door_id",
                table: "abwab_door_inclusions",
                column: "source_door_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_inclusions_target_door_id_source_door_id",
                table: "abwab_door_inclusions",
                columns: new[] { "target_door_id", "source_door_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_linking_source_contributions_door_inclusion",
                table: "linking_source_contributions",
                column: "door_inclusion_id",
                principalTable: "abwab_door_inclusions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_linking_source_contributions_door_inclusion",
                table: "linking_source_contributions");

            migrationBuilder.DropTable(
                name: "abwab_door_inclusion_unit_syncs");

            migrationBuilder.DropTable(
                name: "abwab_door_inclusions");

            migrationBuilder.DropIndex(
                name: "IX_linking_source_contributions_door_inclusion_id",
                table: "linking_source_contributions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_source_contributions_kind_reference_coherence",
                table: "linking_source_contributions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_source_contributions_operation_ownership_coherence",
                table: "linking_source_contributions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_source_contributions_source_kind",
                table: "linking_source_contributions");

            migrationBuilder.DropColumn(
                name: "door_inclusion_id",
                table: "linking_source_contributions");

            migrationBuilder.AlterColumn<long>(
                name: "operation_id",
                table: "linking_source_contributions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_source_contributions_kind_reference_coherence",
                table: "linking_source_contributions",
                sql: "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_source_contributions_source_kind",
                table: "linking_source_contributions",
                sql: "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
        }
    }
}
