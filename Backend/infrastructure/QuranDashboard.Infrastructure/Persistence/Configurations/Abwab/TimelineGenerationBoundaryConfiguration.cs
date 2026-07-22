using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class TimelineGenerationBoundaryConfiguration : IEntityTypeConfiguration<TimelineGenerationBoundary>
{
    public void Configure(EntityTypeBuilder<TimelineGenerationBoundary> builder)
    {
        builder.ToTable("abwab_timeline_generation_boundaries");

        builder.HasKey(b => b.Generation);
        builder.Property(b => b.Generation)
            .ValueGeneratedNever()
            .HasColumnName("generation");

        builder.Property(b => b.IsRoot)
            .IsRequired()
            .HasColumnName("is_root");

        builder.Property(b => b.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(b => b.Reason)
            .HasColumnName("reason");

        // At most one root can ever exist: a duplicate gen-zero/root insert violates this partial index.
        builder.HasIndex(b => b.IsRoot)
            .IsUnique()
            .HasFilter("\"is_root\"");

        // The single immutable generation-zero root, seeded at migration. Root edit/delete is additionally
        // blocked at the DB by a trigger (added in the migration); only 033 inserts non-root boundaries.
        builder.HasData(new TimelineGenerationBoundary
        {
            Generation = TimelineGenerationBoundary.RootGeneration,
            IsRoot = true,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 22, 0, 0, 0, TimeSpan.Zero),
            Reason = "genesis",
        });
    }
}
