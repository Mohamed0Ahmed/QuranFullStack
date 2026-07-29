using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabTemplateConfiguration : IEntityTypeConfiguration<AbwabTemplate>
{
    public void Configure(EntityTypeBuilder<AbwabTemplate> builder)
    {
        builder.ToTable("abwab_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(t => t.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(t => t.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(t => t.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(t => t.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by");

        // uint + IsRowVersion() maps directly to Postgres's xmin system column — no HasColumnName,
        // since giving it one would make EF treat it as a real column and add it to migrations.
        builder.Property(t => t.Version)
            .IsRowVersion();

        builder.HasIndex(t => t.DeletedAtUtc);
    }
}
