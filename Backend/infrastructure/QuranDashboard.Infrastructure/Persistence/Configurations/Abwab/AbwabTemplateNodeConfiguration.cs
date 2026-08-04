using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabTemplateNodeConfiguration : IEntityTypeConfiguration<AbwabTemplateNode>
{
    public void Configure(EntityTypeBuilder<AbwabTemplateNode> builder)
    {
        builder.ToTable("abwab_template_nodes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(n => n.TemplateId)
            .IsRequired()
            .HasColumnName("template_id");

        builder.Property(n => n.ParentNodeId)
            .HasColumnName("parent_node_id");

        builder.Property(n => n.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(n => n.Description)
            .HasColumnName("description");

        builder.Property(n => n.RepresentativeAyahText)
            .HasColumnName("representative_ayah_text");

        builder.Property(n => n.Aliases)
            .IsRequired()
            .HasColumnType("text[]")
            .HasColumnName("aliases");

        builder.Property(n => n.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(n => n.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(n => n.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(n => n.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(n => n.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(n => n.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(n => n.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(n => n.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(n => n.Version)
            .IsRowVersion();

        builder.HasOne<AbwabTemplate>()
            .WithMany()
            .HasForeignKey(n => n.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabTemplateNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.TemplateId)
            .IsUnique()
            .HasFilter("parent_node_id IS NULL AND deleted_at IS NULL");

        builder.HasIndex(n => new { n.TemplateId, n.ParentNodeId, n.Name })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(n => new { n.TemplateId, n.ParentNodeId, n.OrderValue });
        builder.HasIndex(n => n.ParentNodeId);
        builder.HasIndex(n => n.DeletedAtUtc);
    }
}
