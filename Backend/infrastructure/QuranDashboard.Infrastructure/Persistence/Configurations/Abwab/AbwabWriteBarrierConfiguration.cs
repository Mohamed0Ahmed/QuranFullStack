using QuranDashboard.Domain.Abwab.Concurrency;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabWriteBarrierConfiguration : IEntityTypeConfiguration<AbwabWriteBarrier>
{
    public void Configure(EntityTypeBuilder<AbwabWriteBarrier> builder)
    {
        builder.ToTable(
            "abwab_write_barrier",
            t => t.HasCheckConstraint("ck_abwab_write_barrier_singleton", "id = 1"));

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(b => b.State)
            .IsRequired()
            .HasColumnName("state");

        // Pessimistic gate: the commit protocol locks this row FOR UPDATE (§6.2 step 3) before evaluating
        // its state, holding the lock through commit.

        // Exactly one row, seeded Writable at migration.
        builder.HasData(new AbwabWriteBarrier
        {
            Id = AbwabWriteBarrier.SingletonId,
            State = AbwabWriteBarrierState.Writable,
        });
    }
}
