using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

namespace QuranDashboard.Tests.Abwab.Kernel._Support;

internal sealed class AbwabKernelTestContext(DbContextOptions<AbwabKernelTestContext> options)
    : DbContext(options)
{
    public DbSet<ChangeSet> ChangeSets => Set<ChangeSet>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<KernelFixtureWriteTarget> Fixtures => Set<KernelFixtureWriteTarget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ChangeSetConfiguration());
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());

        modelBuilder.Entity<KernelFixtureWriteTarget>(builder =>
        {
            builder.ToTable("abwab_kernel_fixture");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Label).HasColumnName("label").IsRequired();
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        });
    }
}
