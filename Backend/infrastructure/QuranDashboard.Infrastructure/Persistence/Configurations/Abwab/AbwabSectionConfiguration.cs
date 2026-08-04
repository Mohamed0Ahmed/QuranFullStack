using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabSectionConfiguration : IEntityTypeConfiguration<AbwabSection>
{
    public void Configure(EntityTypeBuilder<AbwabSection> builder)
    {
        builder.ToTable("abwab_sections");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(s => s.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(s => s.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(s => s.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(s => s.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(s => s.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(s => s.Version)
            .IsRowVersion();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(s => s.DeletedAtUtc);
    }
}
