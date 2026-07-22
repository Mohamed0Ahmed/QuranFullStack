using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabRevisionStateConfiguration : IEntityTypeConfiguration<AbwabRevisionState>
{
    public void Configure(EntityTypeBuilder<AbwabRevisionState> builder)
    {
        builder.ToTable(
            "abwab_revision_state",
            t => t.HasCheckConstraint("ck_abwab_revision_state_singleton", "id = 1"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(s => s.AuditHeadSequence)
            .IsRequired()
            .HasColumnName("audit_head_sequence");

        builder.Property(s => s.TimelineGeneration)
            .IsRequired()
            .HasColumnName("timeline_generation");

        builder.Property(s => s.TreeRevision)
            .IsRequired()
            .HasColumnName("tree_revision");

        // Concurrency is pessimistic: the commit protocol takes a FOR UPDATE row lock on this singleton and
        // holds it through commit, serializing the head advance.

        builder.HasData(new AbwabRevisionState
        {
            Id = AbwabRevisionState.SingletonId,
            AuditHeadSequence = 0,
            TimelineGeneration = 0,
            TreeRevision = 0,
        });
    }
}
