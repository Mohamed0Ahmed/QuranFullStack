using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorConfiguration : IEntityTypeConfiguration<AbwabDoor>
{
    public void Configure(EntityTypeBuilder<AbwabDoor> builder)
    {
        builder.ToTable("abwab_doors");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(d => d.SectionId)
            .IsRequired()
            .HasColumnName("section_id");

        builder.Property(d => d.ParentId)
            .HasColumnName("parent_id");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(d => d.Description)
            .HasColumnName("description");

        builder.Property(d => d.RepresentativeAyahText)
            .HasColumnName("representative_ayah_text");

        builder.Property(d => d.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(d => d.GlobalOrderValue)
            .HasColumnName("global_order_value");

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(d => d.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(d => d.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(d => d.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(d => d.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(d => d.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(d => d.Version)
            .IsRowVersion();

        builder.HasOne<AbwabSection>()
            .WithMany()
            .HasForeignKey(d => d.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.SectionId, d.ParentId, d.OrderValue });
        builder.HasIndex(d => d.ParentId);
        builder.HasIndex(d => d.DeletedAtUtc);

        builder.HasIndex(d => d.GlobalOrderValue)
            .HasFilter("parent_id IS NULL AND deleted_at IS NULL");

        builder.HasIndex(d => new { d.SectionId, d.ParentId, d.Name })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL");
    }
}
