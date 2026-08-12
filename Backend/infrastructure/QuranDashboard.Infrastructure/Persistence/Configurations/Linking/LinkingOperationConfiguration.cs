using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingOperationConfiguration : IEntityTypeConfiguration<LinkingOperation>
{
    public void Configure(EntityTypeBuilder<LinkingOperation> builder)
    {
        builder.ToTable("linking_operations", table =>
            table.HasCheckConstraint(
                "ck_linking_operations_outcome_schema_version",
                LinkingDescriptorCheckConstraints.JsonbSchemaVersion("outcome")));

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

        builder.HasIndex(operation => operation.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(operation => new { operation.DoorId, operation.ConfirmedAtUtc })
            .IsDescending(false, true);
    }
}
