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

        builder.HasIndex(b => b.IsRoot)
            .IsUnique()
            .HasFilter("\"is_root\"");

        builder.HasData(new TimelineGenerationBoundary
        {
            Generation = TimelineGenerationBoundary.RootGeneration,
            IsRoot = true,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 22, 0, 0, 0, TimeSpan.Zero),
            Reason = "genesis",
        });
    }
}
