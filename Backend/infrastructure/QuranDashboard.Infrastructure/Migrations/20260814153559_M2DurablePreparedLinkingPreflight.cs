using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2DurablePreparedLinkingPreflight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "linking_data_state",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    generation = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_data_state", x => x.id);
                    table.CheckConstraint("ck_linking_data_state_generation", "generation > 0");
                    table.CheckConstraint("ck_linking_data_state_singleton", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_preflights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: false),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    preparation_key = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    stage = table.Column<string>(type: "text", nullable: false),
                    request_schema_version = table.Column<int>(type: "integer", nullable: false),
                    request_document = table.Column<string>(type: "jsonb", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    intent_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    linking_data_revision = table.Column<long>(type: "bigint", nullable: false),
                    expected_door_version = table.Column<long>(type: "bigint", nullable: true),
                    preflight_token = table.Column<string>(type: "text", nullable: true),
                    is_no_op = table.Column<bool>(type: "boolean", nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: true),
                    requested_count = table.Column<int>(type: "integer", nullable: true),
                    new_count = table.Column<int>(type: "integer", nullable: true),
                    overlapping_count = table.Column<int>(type: "integer", nullable: true),
                    unchanged_count = table.Column<int>(type: "integer", nullable: true),
                    updated_count = table.Column<int>(type: "integer", nullable: true),
                    removed_count = table.Column<int>(type: "integer", nullable: true),
                    invalid_count = table.Column<int>(type: "integer", nullable: true),
                    processed_sources = table.Column<int>(type: "integer", nullable: false),
                    total_sources = table.Column<int>(type: "integer", nullable: false),
                    processed_ayahs = table.Column<int>(type: "integer", nullable: false),
                    total_ayahs = table.Column<int>(type: "integer", nullable: true),
                    cancellation_requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmation_accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_owner = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    cleanup_owner = table.Column<Guid>(type: "uuid", nullable: true),
                    cleanup_lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cleanup_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    cleanup_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ready_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failure_code = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_preflights", x => x.id);
                    table.UniqueConstraint("AK_linking_prepared_preflights_id_actor_user_id_door_id", x => new { x.id, x.actor_user_id, x.door_id });
                    table.CheckConstraint("ck_linking_prepared_preflights_failure_code", "failure_code IS NULL OR failure_code IN ('LINKING_DATA_STALE', 'SOURCE_VIEW_STALE', 'WORKSPACE_SOURCE_STALE', 'PREFLIGHT_NOT_READY', 'PREFLIGHT_BLOCKED', 'PREFLIGHT_STALE', 'PREFLIGHT_EXPIRED', 'PREFLIGHT_CANCELLED', 'PREPARATION_FAILED', 'PREFLIGHT_ALREADY_CONFIRMED', 'PREPARATION_ABANDONED', 'CONFIRMATION_CANCELLED', 'CONFIRMATION_FAILED', 'ACTIVE_LINKING_WORKFLOW_LIMIT', 'IDEMPOTENCY_CONFLICT', 'CANCELLATION_TOO_LATE')");
                    table.CheckConstraint("ck_linking_prepared_preflights_intent_hash", "intent_hash IS NULL OR intent_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_linking_prepared_preflights_progress", "processed_sources >= 0 AND total_sources >= 0 AND processed_ayahs >= 0 AND (total_ayahs IS NULL OR total_ayahs >= 0) AND attempt_count >= 0 AND cleanup_attempt_count >= 0");
                    table.CheckConstraint("ck_linking_prepared_preflights_request_document", "jsonb_typeof(request_document) = 'object'\nAND jsonb_exists(request_document, 'schemaVersion')\nAND jsonb_typeof(request_document -> 'schemaVersion') = 'number'\nAND (request_document ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (request_document ->> 'schemaVersion')::numeric <= 2147483647\nAND (request_document ->> 'schemaVersion')::integer = request_schema_version");
                    table.CheckConstraint("ck_linking_prepared_preflights_request_hash", "request_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_linking_prepared_preflights_revision", "linking_data_revision > 0");
                    table.CheckConstraint("ck_linking_prepared_preflights_stage", "stage IN ('resolving', 'classifying', 'persisting')");
                    table.CheckConstraint("ck_linking_prepared_preflights_status", "status IN ('queued', 'preparing', 'ready', 'stale', 'failed', 'cancelled', 'expired', 'confirmed')");
                    table.ForeignKey(
                        name: "FK_linking_prepared_preflights_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_prepared_preflights_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_affected_contributions",
                columns: table => new
                {
                    preflight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_id = table.Column<long>(type: "bigint", nullable: false),
                    expected_contribution_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_affected_contributions", x => new { x.preflight_id, x.contribution_id });
                    table.ForeignKey(
                        name: "FK_linking_prepared_affected_contributions_linking_prepared_pr~",
                        column: x => x.preflight_id,
                        principalTable: "linking_prepared_preflights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_sources",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    preflight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    resolution_identity = table.Column<string>(type: "text", nullable: false),
                    resolution_identity_hash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    contribution_identity = table.Column<string>(type: "text", nullable: false),
                    contribution_identity_hash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    contribution_mode = table.Column<string>(type: "text", nullable: false),
                    descriptor_schema_version = table.Column<int>(type: "integer", nullable: false),
                    descriptor_document = table.Column<string>(type: "jsonb", nullable: false),
                    configuration_schema_version = table.Column<int>(type: "integer", nullable: false),
                    configuration_document = table.Column<string>(type: "jsonb", nullable: false),
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: true),
                    source_version = table.Column<long>(type: "bigint", nullable: true),
                    automatic_word_matches_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    existing_contribution_id = table.Column<long>(type: "bigint", nullable: true),
                    expected_contribution_version = table.Column<long>(type: "bigint", nullable: true),
                    classification = table.Column<string>(type: "text", nullable: true),
                    requested_count = table.Column<int>(type: "integer", nullable: true),
                    new_count = table.Column<int>(type: "integer", nullable: true),
                    overlapping_count = table.Column<int>(type: "integer", nullable: true),
                    unchanged_count = table.Column<int>(type: "integer", nullable: true),
                    updated_count = table.Column<int>(type: "integer", nullable: true),
                    removed_count = table.Column<int>(type: "integer", nullable: true),
                    invalid_count = table.Column<int>(type: "integer", nullable: true),
                    total_ayah_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_sources", x => x.id);
                    table.UniqueConstraint("AK_linking_prepared_sources_id_preflight_id", x => new { x.id, x.preflight_id });
                    table.CheckConstraint("ck_linking_prepared_sources_classification", "classification IS NULL OR classification IN ('NEW_SOURCE', 'NEW_AYAH', 'OVERLAP_OTHER_SOURCE', 'UNCHANGED', 'UPDATE', 'REMOVE', 'INVALID')");
                    table.CheckConstraint("ck_linking_prepared_sources_configuration", "jsonb_typeof(configuration_document) = 'object'\nAND jsonb_exists(configuration_document, 'schemaVersion')\nAND jsonb_typeof(configuration_document -> 'schemaVersion') = 'number'\nAND (configuration_document ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (configuration_document ->> 'schemaVersion')::numeric <= 2147483647\nAND (configuration_document ->> 'schemaVersion')::integer = configuration_schema_version");
                    table.CheckConstraint("ck_linking_prepared_sources_contribution_hash", "octet_length(contribution_identity_hash) = 32");
                    table.CheckConstraint("ck_linking_prepared_sources_counts", "(requested_count IS NULL OR requested_count >= 0) AND (new_count IS NULL OR new_count >= 0) AND (overlapping_count IS NULL OR overlapping_count >= 0) AND (unchanged_count IS NULL OR unchanged_count >= 0) AND (updated_count IS NULL OR updated_count >= 0) AND (removed_count IS NULL OR removed_count >= 0) AND (invalid_count IS NULL OR invalid_count >= 0) AND (total_ayah_count IS NULL OR total_ayah_count >= 0)");
                    table.CheckConstraint("ck_linking_prepared_sources_descriptor", "jsonb_typeof(descriptor_document) = 'object'\nAND jsonb_exists(descriptor_document, 'schemaVersion')\nAND jsonb_typeof(descriptor_document -> 'schemaVersion') = 'number'\nAND (descriptor_document ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (descriptor_document ->> 'schemaVersion')::numeric <= 2147483647\nAND (descriptor_document ->> 'schemaVersion')::integer = descriptor_schema_version");
                    table.CheckConstraint("ck_linking_prepared_sources_kind", "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
                    table.CheckConstraint("ck_linking_prepared_sources_mode", "contribution_mode IN ('automatic', 'manual_single', 'manual_independent', 'manual_grouped')");
                    table.CheckConstraint("ck_linking_prepared_sources_order", "order_value > 0");
                    table.CheckConstraint("ck_linking_prepared_sources_resolution_hash", "octet_length(resolution_identity_hash) = 32");
                    table.ForeignKey(
                        name: "FK_linking_prepared_sources_linking_prepared_preflights_prefli~",
                        column: x => x.preflight_id,
                        principalTable: "linking_prepared_preflights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_units",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    preflight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    unit_identity = table.Column<string>(type: "text", nullable: false),
                    unit_identity_hash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    is_grouped = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_units", x => x.id);
                    table.UniqueConstraint("AK_linking_prepared_units_id_source_id_preflight_id", x => new { x.id, x.source_id, x.preflight_id });
                    table.CheckConstraint("ck_linking_prepared_units_identity_hash", "octet_length(unit_identity_hash) = 32");
                    table.CheckConstraint("ck_linking_prepared_units_order", "order_value > 0");
                    table.ForeignKey(
                        name: "FK_linking_prepared_units_linking_prepared_sources_source_id_p~",
                        columns: x => new { x.source_id, x.preflight_id },
                        principalTable: "linking_prepared_sources",
                        principalColumns: new[] { "id", "preflight_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_ayahs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    preflight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: true),
                    is_requested = table.Column<bool>(type: "boolean", nullable: false),
                    source_order = table.Column<int>(type: "integer", nullable: false),
                    unit_order = table.Column<int>(type: "integer", nullable: false),
                    ayah_order = table.Column<int>(type: "integer", nullable: false),
                    quran_order = table.Column<int>(type: "integer", nullable: false),
                    is_grouped = table.Column<bool>(type: "boolean", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    classification = table.Column<string>(type: "text", nullable: false),
                    invalid_reason = table.Column<string>(type: "text", nullable: true),
                    classification_impact = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_ayahs", x => x.id);
                    table.CheckConstraint("ck_linking_prepared_ayahs_classification", "classification IN ('NEW_SOURCE', 'NEW_AYAH', 'OVERLAP_OTHER_SOURCE', 'UNCHANGED', 'UPDATE', 'REMOVE', 'INVALID')");
                    table.CheckConstraint("ck_linking_prepared_ayahs_impact", "jsonb_typeof(classification_impact) = 'object'\nAND jsonb_exists(classification_impact, 'schemaVersion')\nAND jsonb_typeof(classification_impact -> 'schemaVersion') = 'number'\nAND (classification_impact ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (classification_impact ->> 'schemaVersion')::numeric <= 2147483647");
                    table.CheckConstraint("ck_linking_prepared_ayahs_invalid_reason", "invalid_reason IS NULL OR invalid_reason IN ('DOOR_ARCHIVED', 'AYAH_OUTSIDE_SOURCE', 'WORD_IS_AYAH_MARKER', 'WORD_OUTSIDE_AYAH')");
                    table.CheckConstraint("ck_linking_prepared_ayahs_order", "source_order > 0 AND unit_order > 0 AND ayah_order > 0 AND quran_order > 0");
                    table.CheckConstraint("ck_linking_prepared_ayahs_requested_unit", "(is_requested AND unit_id IS NOT NULL) OR (NOT is_requested AND unit_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_linking_prepared_ayahs_linking_prepared_sources_source_id_p~",
                        columns: x => new { x.source_id, x.preflight_id },
                        principalTable: "linking_prepared_sources",
                        principalColumns: new[] { "id", "preflight_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_prepared_ayahs_linking_prepared_units_unit_id_sourc~",
                        columns: x => new { x.unit_id, x.source_id, x.preflight_id },
                        principalTable: "linking_prepared_units",
                        principalColumns: new[] { "id", "source_id", "preflight_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_ayah_descriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prepared_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_ayah_descriptions", x => x.id);
                    table.CheckConstraint("ck_linking_prepared_ayah_descriptions_body", "btrim(body) <> ''");
                    table.CheckConstraint("ck_linking_prepared_ayah_descriptions_order", "order_value BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_linking_prepared_ayah_descriptions_linking_prepared_ayahs_p~",
                        column: x => x.prepared_ayah_id,
                        principalTable: "linking_prepared_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_prepared_ayah_words",
                columns: table => new
                {
                    prepared_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    is_source_match = table.Column<bool>(type: "boolean", nullable: false),
                    is_requested = table.Column<bool>(type: "boolean", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_prepared_ayah_words", x => new { x.prepared_ayah_id, x.quran_word_id });
                    table.CheckConstraint("ck_linking_prepared_ayah_words_membership", "is_source_match OR is_requested");
                    table.CheckConstraint("ck_linking_prepared_ayah_words_order", "order_value > 0");
                    table.ForeignKey(
                        name: "FK_linking_prepared_ayah_words_linking_prepared_ayahs_prepared~",
                        column: x => x.prepared_ayah_id,
                        principalTable: "linking_prepared_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "linking_data_state",
                columns: new[] { "id", "generation", "updated_at_utc" },
                values: new object[] { (short)1, 1L, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayah_descriptions_prepared_ayah_id_order_v~",
                table: "linking_prepared_ayah_descriptions",
                columns: new[] { "prepared_ayah_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayah_words_prepared_ayah_id_order_value",
                table: "linking_prepared_ayah_words",
                columns: new[] { "prepared_ayah_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_preflight_id_ayah_id_source_order",
                table: "linking_prepared_ayahs",
                columns: new[] { "preflight_id", "ayah_id", "source_order" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_preflight_id_classification_quran_or~",
                table: "linking_prepared_ayahs",
                columns: new[] { "preflight_id", "classification", "quran_order", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_preflight_id_quran_order_ayah_id",
                table: "linking_prepared_ayahs",
                columns: new[] { "preflight_id", "quran_order", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_source_id_ayah_id",
                table: "linking_prepared_ayahs",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_source_id_classification_quran_order~",
                table: "linking_prepared_ayahs",
                columns: new[] { "source_id", "classification", "quran_order", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_source_id_preflight_id",
                table: "linking_prepared_ayahs",
                columns: new[] { "source_id", "preflight_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_source_id_quran_order_ayah_id",
                table: "linking_prepared_ayahs",
                columns: new[] { "source_id", "quran_order", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_ayahs_unit_id_source_id_preflight_id",
                table: "linking_prepared_ayahs",
                columns: new[] { "unit_id", "source_id", "preflight_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_actor_user_id_id",
                table: "linking_prepared_preflights",
                columns: new[] { "actor_user_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_actor_user_id_preparation_key",
                table: "linking_prepared_preflights",
                columns: new[] { "actor_user_id", "preparation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_cleanup_lease_expires_at_utc_id",
                table: "linking_prepared_preflights",
                columns: new[] { "cleanup_lease_expires_at_utc", "id" },
                filter: "cleanup_started_at_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_completed_at_utc_id",
                table: "linking_prepared_preflights",
                columns: new[] { "completed_at_utc", "id" },
                filter: "status IN ('stale', 'failed', 'cancelled', 'expired', 'confirmed') AND cleanup_started_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_door_id",
                table: "linking_prepared_preflights",
                column: "door_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_expires_at_utc_id",
                table: "linking_prepared_preflights",
                columns: new[] { "expires_at_utc", "id" },
                filter: "status = 'ready' AND confirmation_accepted_at_utc IS NULL AND cleanup_started_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_preflights_status_lease_expires_at_utc_cre~",
                table: "linking_prepared_preflights",
                columns: new[] { "status", "lease_expires_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_sources_preflight_id_contribution_identity~",
                table: "linking_prepared_sources",
                columns: new[] { "preflight_id", "contribution_identity_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_sources_preflight_id_order_value",
                table: "linking_prepared_sources",
                columns: new[] { "preflight_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_sources_preflight_id_resolution_identity_h~",
                table: "linking_prepared_sources",
                columns: new[] { "preflight_id", "resolution_identity_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_units_source_id_order_value",
                table: "linking_prepared_units",
                columns: new[] { "source_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_units_source_id_preflight_id",
                table: "linking_prepared_units",
                columns: new[] { "source_id", "preflight_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_prepared_units_source_id_unit_identity_hash",
                table: "linking_prepared_units",
                columns: new[] { "source_id", "unit_identity_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linking_data_state");

            migrationBuilder.DropTable(
                name: "linking_prepared_affected_contributions");

            migrationBuilder.DropTable(
                name: "linking_prepared_ayah_descriptions");

            migrationBuilder.DropTable(
                name: "linking_prepared_ayah_words");

            migrationBuilder.DropTable(
                name: "linking_prepared_ayahs");

            migrationBuilder.DropTable(
                name: "linking_prepared_units");

            migrationBuilder.DropTable(
                name: "linking_prepared_sources");

            migrationBuilder.DropTable(
                name: "linking_prepared_preflights");
        }
    }
}
