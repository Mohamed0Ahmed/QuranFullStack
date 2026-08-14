using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingConfirmationJobConfiguration : IEntityTypeConfiguration<LinkingConfirmationJob>
{
    public void Configure(EntityTypeBuilder<LinkingConfirmationJob> builder)
    {
        builder.ToTable("linking_confirmation_jobs", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_status",
                LinkingDescriptorCheckConstraints.TokenIn("status", LinkingConfirmationJobLifecycleTokens.StatusTokens));
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_stage",
                LinkingDescriptorCheckConstraints.TokenIn("stage", LinkingConfirmationJobLifecycleTokens.StageTokens));
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_failure_code",
                "failure_code IS NULL OR " + LinkingDescriptorCheckConstraints.TokenIn(
                    "failure_code", LinkingConfirmationJobLifecycleTokens.FailureCodeTokens));
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_request_hash",
                LinkingPreparedSchemaConstraints.RequiredHexHash("request_hash"));
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_outcome_document",
                "outcome_document IS NULL OR (" + LinkingDescriptorCheckConstraints.JsonbSchemaVersion("outcome_document") + ")");
            table.HasCheckConstraint(
                "ck_linking_confirmation_jobs_progress",
                "processed_items >= 0 AND total_items >= 0 AND processed_items <= total_items "
                + "AND attempt_count >= 0 AND cleanup_attempt_count >= 0");
        });

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(job => job.PreflightId).IsRequired().HasColumnName("preflight_id");
        builder.Property(job => job.ActorUserId).IsRequired().HasColumnName("actor_user_id");
        builder.Property(job => job.DoorId).IsRequired().HasColumnName("door_id");
        builder.Property(job => job.IdempotencyKey).IsRequired().HasColumnName("idempotency_key");
        builder.Property(job => job.RequestHash).IsRequired().HasMaxLength(64).HasColumnName("request_hash");
        builder.Property(job => job.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion(
                status => LinkingConfirmationJobLifecycleTokens.ToToken(status),
                token => LinkingConfirmationJobLifecycleTokens.ParseStatus(token));
        builder.Property(job => job.Stage)
            .IsRequired()
            .HasColumnName("stage")
            .HasConversion(
                stage => LinkingConfirmationJobLifecycleTokens.ToToken(stage),
                token => LinkingConfirmationJobLifecycleTokens.ParseStage(token));
        builder.Property(job => job.ProcessedItems).IsRequired().HasColumnName("processed_items");
        builder.Property(job => job.TotalItems).IsRequired().HasColumnName("total_items");
        builder.Property(job => job.CancellationRequestedAtUtc).HasColumnName("cancellation_requested_at_utc");
        builder.Property(job => job.AttemptCount).IsRequired().HasColumnName("attempt_count");
        builder.Property(job => job.LeaseOwner).HasColumnName("lease_owner");
        builder.Property(job => job.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        builder.Property(job => job.CleanupOwner).HasColumnName("cleanup_owner");
        builder.Property(job => job.CleanupLeaseExpiresAtUtc).HasColumnName("cleanup_lease_expires_at_utc");
        builder.Property(job => job.CleanupAttemptCount).IsRequired().HasColumnName("cleanup_attempt_count");
        builder.Property(job => job.CleanupStartedAtUtc).HasColumnName("cleanup_started_at_utc");
        builder.Property(job => job.QueuedAtUtc).IsRequired().HasColumnName("queued_at_utc");
        builder.Property(job => job.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(job => job.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(job => job.UpdatedAtUtc).IsRequired().HasColumnName("updated_at_utc");
        builder.Property(job => job.OperationId).HasColumnName("operation_id");
        builder.Property(job => job.OutcomeDocumentJson).HasColumnType("jsonb").HasColumnName("outcome_document");
        builder.Property(job => job.FailureCode)
            .HasColumnName("failure_code")
            .HasConversion(
                failureCode => LinkingConfirmationJobLifecycleTokens.ToToken(failureCode),
                token => LinkingConfirmationJobLifecycleTokens.ParseFailureCode(token));
        builder.Property(job => job.Version).IsRowVersion();

        builder.HasOne<LinkingPreparedPreflight>()
            .WithMany()
            .HasForeignKey(job => new { job.PreflightId, job.ActorUserId, job.DoorId })
            .HasPrincipalKey(preflight => new { preflight.Id, preflight.ActorUserId, preflight.DoorId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(job => job.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(job => job.DoorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LinkingOperation>()
            .WithMany()
            .HasForeignKey(job => job.OperationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(job => job.IdempotencyKey).IsUnique();
        builder.HasIndex(job => job.PreflightId).IsUnique();
        builder.HasIndex(job => job.OperationId).IsUnique();
        builder.HasIndex(job => new { job.Status, job.LeaseExpiresAtUtc, job.QueuedAtUtc });
        builder.HasIndex(job => new { job.DoorId, job.Status });
        builder.HasIndex(job => job.DoorId)
            .IsUnique()
            .HasFilter("status IN ('running', 'finalizing')");
        builder.HasIndex(job => new { job.CompletedAtUtc, job.Id })
            .HasFilter(
                "status IN ('succeeded', 'stale', 'failed', 'cancelled') "
                + "AND cleanup_started_at_utc IS NULL");
        builder.HasIndex(job => new { job.CleanupLeaseExpiresAtUtc, job.Id })
            .HasFilter("cleanup_started_at_utc IS NOT NULL");
    }
}
