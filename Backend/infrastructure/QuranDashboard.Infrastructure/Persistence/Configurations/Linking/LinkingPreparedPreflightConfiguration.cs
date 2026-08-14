using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedPreflightConfiguration : IEntityTypeConfiguration<LinkingPreparedPreflight>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedPreflight> builder)
    {
        builder.ToTable("linking_prepared_preflights", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_status",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "status", LinkingPreparedPreflightLifecycleTokens.StatusTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_stage",
                LinkingDescriptorCheckConstraints.TokenIn(
                    "stage", LinkingPreparedPreflightLifecycleTokens.StageTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_failure_code",
                "failure_code IS NULL OR " + LinkingDescriptorCheckConstraints.TokenIn(
                    "failure_code", LinkingPreparedPreflightLifecycleTokens.FailureCodeTokens));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_request_document",
                LinkingPreparedSchemaConstraints.JsonbSchemaVersionMatches(
                    "request_document", "request_schema_version"));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_request_hash",
                LinkingPreparedSchemaConstraints.RequiredHexHash("request_hash"));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_intent_hash",
                LinkingPreparedSchemaConstraints.OptionalHexHash("intent_hash"));
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_revision",
                "linking_data_revision > 0");
            table.HasCheckConstraint(
                "ck_linking_prepared_preflights_progress",
                "processed_sources >= 0 AND total_sources >= 0 AND processed_ayahs >= 0 "
                + "AND (total_ayahs IS NULL OR total_ayahs >= 0) "
                + "AND attempt_count >= 0 AND cleanup_attempt_count >= 0");
        });

        builder.HasKey(preflight => preflight.Id);
        builder.Property(preflight => preflight.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(preflight => preflight.ActorUserId).IsRequired().HasColumnName("actor_user_id");
        builder.Property(preflight => preflight.DoorId).IsRequired().HasColumnName("door_id");
        builder.Property(preflight => preflight.PreparationKey).IsRequired().HasColumnName("preparation_key");
        builder.Property(preflight => preflight.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion(
                status => LinkingPreparedPreflightLifecycleTokens.ToToken(status),
                token => LinkingPreparedPreflightLifecycleTokens.ParseStatus(token));
        builder.Property(preflight => preflight.Stage)
            .IsRequired()
            .HasColumnName("stage")
            .HasConversion(
                stage => LinkingPreparedPreflightLifecycleTokens.ToToken(stage),
                token => LinkingPreparedPreflightLifecycleTokens.ParseStage(token));
        builder.Property(preflight => preflight.RequestSchemaVersion)
            .IsRequired()
            .HasColumnName("request_schema_version");
        builder.Property(preflight => preflight.RequestDocumentJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("request_document");
        builder.Property(preflight => preflight.RequestHash).IsRequired().HasMaxLength(64).HasColumnName("request_hash");
        builder.Property(preflight => preflight.IntentHash).HasMaxLength(64).HasColumnName("intent_hash");
        builder.Property(preflight => preflight.LinkingDataRevision)
            .IsRequired()
            .HasColumnName("linking_data_revision");
        builder.Property(preflight => preflight.ExpectedDoorVersion).HasColumnName("expected_door_version");
        builder.Property(preflight => preflight.PreflightToken).HasColumnName("preflight_token");
        builder.Property(preflight => preflight.IsNoOp).HasColumnName("is_no_op");
        builder.Property(preflight => preflight.IsBlocked).HasColumnName("is_blocked");
        builder.Property(preflight => preflight.RequestedCount).HasColumnName("requested_count");
        builder.Property(preflight => preflight.NewCount).HasColumnName("new_count");
        builder.Property(preflight => preflight.OverlappingCount).HasColumnName("overlapping_count");
        builder.Property(preflight => preflight.UnchangedCount).HasColumnName("unchanged_count");
        builder.Property(preflight => preflight.UpdatedCount).HasColumnName("updated_count");
        builder.Property(preflight => preflight.RemovedCount).HasColumnName("removed_count");
        builder.Property(preflight => preflight.InvalidCount).HasColumnName("invalid_count");
        builder.Property(preflight => preflight.ProcessedSources).IsRequired().HasColumnName("processed_sources");
        builder.Property(preflight => preflight.TotalSources).IsRequired().HasColumnName("total_sources");
        builder.Property(preflight => preflight.ProcessedAyahs).IsRequired().HasColumnName("processed_ayahs");
        builder.Property(preflight => preflight.TotalAyahs).HasColumnName("total_ayahs");
        builder.Property(preflight => preflight.CancellationRequestedAtUtc)
            .HasColumnName("cancellation_requested_at_utc");
        builder.Property(preflight => preflight.ConfirmationAcceptedAtUtc)
            .HasColumnName("confirmation_accepted_at_utc");
        builder.Property(preflight => preflight.LeaseOwner).HasColumnName("lease_owner");
        builder.Property(preflight => preflight.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        builder.Property(preflight => preflight.AttemptCount).IsRequired().HasColumnName("attempt_count");
        builder.Property(preflight => preflight.CleanupOwner).HasColumnName("cleanup_owner");
        builder.Property(preflight => preflight.CleanupLeaseExpiresAtUtc)
            .HasColumnName("cleanup_lease_expires_at_utc");
        builder.Property(preflight => preflight.CleanupAttemptCount)
            .IsRequired()
            .HasColumnName("cleanup_attempt_count");
        builder.Property(preflight => preflight.CleanupStartedAtUtc).HasColumnName("cleanup_started_at_utc");
        builder.Property(preflight => preflight.CreatedAtUtc).IsRequired().HasColumnName("created_at_utc");
        builder.Property(preflight => preflight.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(preflight => preflight.ReadyAtUtc).HasColumnName("ready_at_utc");
        builder.Property(preflight => preflight.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(preflight => preflight.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(preflight => preflight.ConfirmedAtUtc).HasColumnName("confirmed_at_utc");
        builder.Property(preflight => preflight.UpdatedAtUtc).IsRequired().HasColumnName("updated_at_utc");
        builder.Property(preflight => preflight.FailureCode)
            .HasColumnName("failure_code")
            .HasConversion(
                failureCode => LinkingPreparedPreflightLifecycleTokens.ToToken(failureCode),
                token => LinkingPreparedPreflightLifecycleTokens.ParseFailureCode(token));
        builder.Property(preflight => preflight.Version).IsRowVersion();

        builder.HasAlternateKey(preflight => new { preflight.Id, preflight.ActorUserId, preflight.DoorId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(preflight => preflight.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(preflight => preflight.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(preflight => new { preflight.ActorUserId, preflight.PreparationKey }).IsUnique();
        builder.HasIndex(preflight => new { preflight.Status, preflight.LeaseExpiresAtUtc, preflight.CreatedAtUtc });
        builder.HasIndex(preflight => new { preflight.ActorUserId, preflight.Id });
        builder.HasIndex(preflight => new { preflight.ExpiresAtUtc, preflight.Id })
            .HasFilter(
                "status = 'ready' AND confirmation_accepted_at_utc IS NULL "
                + "AND cleanup_started_at_utc IS NULL");
        builder.HasIndex(preflight => new { preflight.CompletedAtUtc, preflight.Id })
            .HasFilter(
                "status IN ('stale', 'failed', 'cancelled', 'expired', 'confirmed') "
                + "AND cleanup_started_at_utc IS NULL");
        builder.HasIndex(preflight => new { preflight.CleanupLeaseExpiresAtUtc, preflight.Id })
            .HasFilter("cleanup_started_at_utc IS NOT NULL");
    }
}
