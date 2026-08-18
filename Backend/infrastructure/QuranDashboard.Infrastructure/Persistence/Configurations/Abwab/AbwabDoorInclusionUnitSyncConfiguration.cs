using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorInclusionUnitSyncConfiguration
    : IEntityTypeConfiguration<AbwabDoorInclusionUnitSync>
{
    private const string Active = "active";
    private const string Overridden = "overridden";
    private const string Suppressed = "suppressed";

    public void Configure(EntityTypeBuilder<AbwabDoorInclusionUnitSync> builder)
    {
        builder.ToTable("abwab_door_inclusion_unit_syncs", table =>
        {
            table.HasCheckConstraint(
                "ck_abwab_door_inclusion_unit_syncs_state",
                $"state IN ('{Active}', '{Overridden}', '{Suppressed}')");
            table.HasCheckConstraint(
                "ck_abwab_door_inclusion_unit_syncs_target_coherence",
                $"(state IN ('{Active}', '{Overridden}') AND target_unit_id IS NOT NULL) "
                + $"OR (state = '{Suppressed}' AND target_unit_id IS NULL)");
        });

        builder.HasKey(sync => sync.Id);
        builder.Property(sync => sync.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(sync => sync.DoorInclusionId)
            .IsRequired()
            .HasColumnName("door_inclusion_id");

        builder.Property(sync => sync.SourceUnitId)
            .IsRequired()
            .HasColumnName("source_unit_id");

        builder.Property(sync => sync.TargetUnitId)
            .HasColumnName("target_unit_id");

        builder.Property(sync => sync.State)
            .IsRequired()
            .HasColumnName("state")
            .HasConversion(
                state => ToToken(state),
                token => FromToken(token));

        builder.Property(sync => sync.SourceFingerprint)
            .IsRequired()
            .HasColumnName("source_fingerprint");

        builder.Property(sync => sync.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(sync => sync.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(sync => sync.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(sync => sync.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasOne<AbwabDoorInclusion>()
            .WithMany()
            .HasForeignKey(sync => sync.DoorInclusionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_abwab_door_inclusion_syncs_inclusion");

        builder.HasOne<LinkingUnit>()
            .WithMany()
            .HasForeignKey(sync => sync.SourceUnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_abwab_door_inclusion_syncs_source_unit");

        builder.HasOne<LinkingUnit>()
            .WithMany()
            .HasForeignKey(sync => sync.TargetUnitId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_abwab_door_inclusion_syncs_target_unit");

        builder.HasIndex(sync => new { sync.DoorInclusionId, sync.SourceUnitId })
            .IsUnique()
            .HasDatabaseName("IX_abwab_door_inclusion_syncs_inclusion_source");

        builder.HasIndex(sync => sync.TargetUnitId)
            .IsUnique()
            .HasFilter("target_unit_id IS NOT NULL")
            .HasDatabaseName("IX_abwab_door_inclusion_syncs_target_unit");

        builder.HasIndex(sync => sync.SourceUnitId)
            .HasDatabaseName("IX_abwab_door_inclusion_syncs_source_unit");
    }

    private static string ToToken(AbwabDoorInclusionSyncState state) =>
        state switch
        {
            AbwabDoorInclusionSyncState.Active => Active,
            AbwabDoorInclusionSyncState.Overridden => Overridden,
            AbwabDoorInclusionSyncState.Suppressed => Suppressed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown door inclusion sync state."),
        };

    private static AbwabDoorInclusionSyncState FromToken(string token) =>
        token switch
        {
            Active => AbwabDoorInclusionSyncState.Active,
            Overridden => AbwabDoorInclusionSyncState.Overridden,
            Suppressed => AbwabDoorInclusionSyncState.Suppressed,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown door inclusion sync state token."),
        };
}
