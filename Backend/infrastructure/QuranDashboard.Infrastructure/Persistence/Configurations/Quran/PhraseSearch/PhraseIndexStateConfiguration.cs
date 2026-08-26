using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class PhraseIndexStateConfiguration : IEntityTypeConfiguration<PhraseIndexState>
{
    public void Configure(EntityTypeBuilder<PhraseIndexState> builder)
    {
        builder.ToTable("quran_phrase_index_state", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_index_state_singleton",
                "id = 1");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_state_source_revision",
                "source_revision >= 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_state_source_fingerprint",
                "source_fingerprint IS NULL OR source_fingerprint ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_state_distinct_builds",
                "active_build_id IS NULL OR previous_build_id IS NULL OR active_build_id <> previous_build_id");
            table.HasCheckConstraint(
                "ck_quran_phrase_index_state_stale_reason",
                "is_stale OR stale_reason IS NULL");
        });

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(state => state.SourceRevision)
            .IsRequired()
            .HasColumnName("source_revision");

        builder.Property(state => state.SourceFingerprint)
            .HasMaxLength(64)
            .HasColumnName("source_fingerprint");

        builder.Property(state => state.ActiveBuildId)
            .HasColumnName("active_build_id");

        builder.Property(state => state.PreviousBuildId)
            .HasColumnName("previous_build_id");

        builder.Property(state => state.IsStale)
            .IsRequired()
            .HasColumnName("is_stale");

        builder.Property(state => state.StaleReason)
            .HasMaxLength(256)
            .HasColumnName("stale_reason");

        builder.Property(state => state.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at_utc");

        builder.HasOne<PhraseIndexBuild>()
            .WithMany()
            .HasForeignKey(state => state.ActiveBuildId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PhraseIndexBuild>()
            .WithMany()
            .HasForeignKey(state => state.PreviousBuildId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasData(new PhraseIndexState
        {
            Id = PhraseIndexState.SingletonId,
            SourceRevision = 0,
            SourceFingerprint = null,
            ActiveBuildId = null,
            PreviousBuildId = null,
            IsStale = false,
            StaleReason = null,
            UpdatedAtUtc = DateTimeOffset.UnixEpoch,
        });
    }
}
