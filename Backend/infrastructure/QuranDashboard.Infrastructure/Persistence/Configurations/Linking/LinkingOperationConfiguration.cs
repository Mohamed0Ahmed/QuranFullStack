using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingOperationConfiguration : IEntityTypeConfiguration<LinkingOperation>
{
    public void Configure(EntityTypeBuilder<LinkingOperation> builder)
    {
        builder.ToTable("linking_operations", table =>
        {
            table.HasCheckConstraint(
                "ck_linking_operations_outcome_schema_version",
                LinkingDescriptorCheckConstraints.JsonbSchemaVersion("outcome"));
            table.HasCheckConstraint(
                "ck_linking_operations_request_hash",
                LinkingPreparedSchemaConstraints.OptionalHexHash("request_hash"));
            table.HasCheckConstraint(
                "ck_linking_operations_request_contract",
                "(request_contract_kind IS NULL AND request_schema_version IS NULL AND request_hash IS NULL "
                + "AND linking_data_revision IS NULL AND prepared_preflight_reference_id IS NULL "
                + "AND confirmation_job_reference_id IS NULL AND prepared_preflight_id IS NULL) OR "
                + "(request_contract_kind = 'prepared_job' AND request_schema_version > 0 "
                + "AND request_hash IS NOT NULL AND linking_data_revision > 0 "
                + "AND prepared_preflight_reference_id IS NOT NULL AND confirmation_job_reference_id IS NOT NULL) OR "
                + "(request_contract_kind = 'legacy_expanded' AND request_schema_version > 0 "
                + "AND request_hash IS NOT NULL AND linking_data_revision > 0 "
                + "AND prepared_preflight_reference_id IS NULL AND confirmation_job_reference_id IS NULL "
                + "AND prepared_preflight_id IS NULL)");
        });

        builder.HasKey(operation => operation.Id);
        builder.Property(operation => operation.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(operation => operation.DoorId)
            .IsRequired()
            .HasColumnName("door_id");

        builder.Property(operation => operation.ActorUserId)
            .IsRequired()
            .HasColumnName("actor_user_id");

        builder.Property(operation => operation.IdempotencyKey)
            .IsRequired()
            .HasColumnName("idempotency_key");

        builder.Property(operation => operation.PreparedPreflightId)
            .HasColumnName("prepared_preflight_id");

        builder.Property(operation => operation.PreparedPreflightReferenceId)
            .HasColumnName("prepared_preflight_reference_id");

        builder.Property(operation => operation.ConfirmationJobReferenceId)
            .HasColumnName("confirmation_job_reference_id");

        builder.Property(operation => operation.RequestContractKind)
            .HasMaxLength(32)
            .HasColumnName("request_contract_kind");

        builder.Property(operation => operation.RequestSchemaVersion)
            .HasColumnName("request_schema_version");

        builder.Property(operation => operation.RequestHash)
            .HasMaxLength(64)
            .HasColumnName("request_hash");

        builder.Property(operation => operation.LinkingDataRevision)
            .HasColumnName("linking_data_revision");

        builder.Property(operation => operation.ConfirmedAtUtc)
            .IsRequired()
            .HasColumnName("confirmed_at");

        builder.Property(operation => operation.SourceCount)
            .IsRequired()
            .HasColumnName("source_count");

        builder.Property(operation => operation.AyahCount)
            .IsRequired()
            .HasColumnName("ayah_count");

        builder.Property(operation => operation.OutcomeJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("outcome");

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(operation => operation.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(operation => operation.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LinkingPreparedPreflight>()
            .WithMany()
            .HasForeignKey(operation => operation.PreparedPreflightId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(operation => new { operation.DoorId, operation.ConfirmedAtUtc })
            .IsDescending(false, true);
    }
}
