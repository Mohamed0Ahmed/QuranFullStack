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

        // Concurrency is enforced pessimistically: the commit protocol takes a FOR UPDATE row lock on this
        // singleton (§6.2 step 4) and holds it through commit, which serializes the head advance. (Npgsql 10
        // no longer ships the xmin concurrency-token helper; the PostgreSQL row's xmin still exists at the DB
        // and the row-lock is the guarantee the convention calls for.)

        // Exactly one row, seeded at migration: head/generation/tree all zero.
        builder.HasData(new AbwabRevisionState
        {
            Id = AbwabRevisionState.SingletonId,
            AuditHeadSequence = 0,
            TimelineGeneration = 0,
            TreeRevision = 0,
        });
    }
}
