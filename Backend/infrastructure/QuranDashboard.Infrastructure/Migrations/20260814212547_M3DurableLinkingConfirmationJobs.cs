using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M3DurableLinkingConfirmationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "confirmation_job_reference_id",
                table: "linking_operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "linking_data_revision",
                table: "linking_operations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "prepared_preflight_id",
                table: "linking_operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "prepared_preflight_reference_id",
                table: "linking_operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_contract_kind",
                table: "linking_operations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_hash",
                table: "linking_operations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "request_schema_version",
                table: "linking_operations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "linking_confirmation_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    preflight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: false),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    stage = table.Column<string>(type: "text", nullable: false),
                    processed_items = table.Column<int>(type: "integer", nullable: false),
                    total_items = table.Column<int>(type: "integer", nullable: false),
                    cancellation_requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    lease_owner = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cleanup_owner = table.Column<Guid>(type: "uuid", nullable: true),
                    cleanup_lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cleanup_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    cleanup_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    queued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    operation_id = table.Column<long>(type: "bigint", nullable: true),
                    outcome_document = table.Column<string>(type: "jsonb", nullable: true),
                    failure_code = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_confirmation_jobs", x => x.id);
                    table.CheckConstraint("ck_linking_confirmation_jobs_failure_code", "failure_code IS NULL OR failure_code IN ('LINKING_DATA_STALE', 'PREFLIGHT_BLOCKED', 'PREFLIGHT_STALE', 'CONFIRMATION_CANCELLED', 'CONFIRMATION_FAILED', 'DOOR_NOT_FOUND', 'IDEMPOTENCY_CONFLICT')");
                    table.CheckConstraint("ck_linking_confirmation_jobs_outcome_document", "outcome_document IS NULL OR (jsonb_typeof(outcome_document) = 'object'\nAND jsonb_exists(outcome_document, 'schemaVersion')\nAND jsonb_typeof(outcome_document -> 'schemaVersion') = 'number'\nAND (outcome_document ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (outcome_document ->> 'schemaVersion')::numeric <= 2147483647)");
                    table.CheckConstraint("ck_linking_confirmation_jobs_progress", "processed_items >= 0 AND total_items >= 0 AND processed_items <= total_items AND attempt_count >= 0 AND cleanup_attempt_count >= 0");
                    table.CheckConstraint("ck_linking_confirmation_jobs_request_hash", "request_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_linking_confirmation_jobs_stage", "stage IN ('loading-prepared', 'applying-unit-diff', 'synchronizing-door', 'committing')");
                    table.CheckConstraint("ck_linking_confirmation_jobs_status", "status IN ('queued', 'running', 'finalizing', 'succeeded', 'stale', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_linking_confirmation_jobs_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_confirmation_jobs_linking_operations_operation_id",
                        column: x => x.operation_id,
                        principalTable: "linking_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_confirmation_jobs_linking_prepared_preflights_prefl~",
                        columns: x => new { x.preflight_id, x.actor_user_id, x.door_id },
                        principalTable: "linking_prepared_preflights",
                        principalColumns: new[] { "id", "actor_user_id", "door_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_confirmation_jobs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_prepared_preflight_id",
                table: "linking_operations",
                column: "prepared_preflight_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_operations_request_contract",
                table: "linking_operations",
                sql: "(request_contract_kind IS NULL AND request_schema_version IS NULL AND request_hash IS NULL AND linking_data_revision IS NULL AND prepared_preflight_reference_id IS NULL AND confirmation_job_reference_id IS NULL AND prepared_preflight_id IS NULL) OR (request_contract_kind = 'prepared_job' AND request_schema_version > 0 AND request_hash IS NOT NULL AND linking_data_revision > 0 AND prepared_preflight_reference_id IS NOT NULL AND confirmation_job_reference_id IS NOT NULL) OR (request_contract_kind = 'legacy_expanded' AND request_schema_version > 0 AND request_hash IS NOT NULL AND linking_data_revision > 0 AND prepared_preflight_reference_id IS NULL AND confirmation_job_reference_id IS NULL AND prepared_preflight_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_linking_operations_request_hash",
                table: "linking_operations",
                sql: "request_hash IS NULL OR request_hash ~ '^[0-9a-f]{64}$'");

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_actor_user_id",
                table: "linking_confirmation_jobs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_cleanup_lease_expires_at_utc_id",
                table: "linking_confirmation_jobs",
                columns: new[] { "cleanup_lease_expires_at_utc", "id" },
                filter: "cleanup_started_at_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_completed_at_utc_id",
                table: "linking_confirmation_jobs",
                columns: new[] { "completed_at_utc", "id" },
                filter: "status IN ('succeeded', 'stale', 'failed', 'cancelled') AND cleanup_started_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_door_id",
                table: "linking_confirmation_jobs",
                column: "door_id",
                unique: true,
                filter: "status IN ('running', 'finalizing')");

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_door_id_status",
                table: "linking_confirmation_jobs",
                columns: new[] { "door_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_idempotency_key",
                table: "linking_confirmation_jobs",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_operation_id",
                table: "linking_confirmation_jobs",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_preflight_id",
                table: "linking_confirmation_jobs",
                column: "preflight_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_preflight_id_actor_user_id_door_id",
                table: "linking_confirmation_jobs",
                columns: new[] { "preflight_id", "actor_user_id", "door_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_confirmation_jobs_status_lease_expires_at_utc_queue~",
                table: "linking_confirmation_jobs",
                columns: new[] { "status", "lease_expires_at_utc", "queued_at_utc" });

            migrationBuilder.AddForeignKey(
                name: "FK_linking_operations_linking_prepared_preflights_prepared_pre~",
                table: "linking_operations",
                column: "prepared_preflight_id",
                principalTable: "linking_prepared_preflights",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_linking_operations_linking_prepared_preflights_prepared_pre~",
                table: "linking_operations");

            migrationBuilder.DropTable(
                name: "linking_confirmation_jobs");

            migrationBuilder.DropIndex(
                name: "IX_linking_operations_prepared_preflight_id",
                table: "linking_operations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_operations_request_contract",
                table: "linking_operations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_linking_operations_request_hash",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "confirmation_job_reference_id",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "linking_data_revision",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "prepared_preflight_id",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "prepared_preflight_reference_id",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "request_contract_kind",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "request_hash",
                table: "linking_operations");

            migrationBuilder.DropColumn(
                name: "request_schema_version",
                table: "linking_operations");
        }
    }
}
