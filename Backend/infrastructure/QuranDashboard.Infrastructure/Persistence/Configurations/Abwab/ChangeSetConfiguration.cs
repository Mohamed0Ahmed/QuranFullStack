using QuranDashboard.Domain.Abwab.Audit;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class ChangeSetConfiguration : IEntityTypeConfiguration<ChangeSet>
{
    public void Configure(EntityTypeBuilder<ChangeSet> builder)
    {
        builder.ToTable("abwab_change_sets");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(c => c.TimelineGeneration)
            .IsRequired()
            .HasColumnName("timeline_generation");

        builder.Property(c => c.ChangeSetSequence)
            .IsRequired()
            .HasColumnName("change_set_sequence");

        builder.Property(c => c.ActorSubject)
            .IsRequired()
            .HasColumnName("actor_subject");

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.HasMany(c => c.Events)
            .WithOne()
            .HasForeignKey(e => e.ChangeSetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ChangeSetSequence).IsUnique();
    }
}
