using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class TemplateNodeConfiguration : IEntityTypeConfiguration<TemplateNode>
{
    public void Configure(EntityTypeBuilder<TemplateNode> builder)
    {
        builder.ToTable("abwab_template_nodes", table =>
            table.HasCheckConstraint(
                "ck_abwab_template_nodes_no_self_parent",
                "parent_template_node_id IS NULL OR parent_template_node_id <> id"));

        builder.HasKey(n => n.TemplateNodeId);
        builder.Property(n => n.TemplateNodeId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(n => n.DoorTemplateId)
            .IsRequired()
            .HasColumnName("door_template_id");

        builder.Property(n => n.ParentTemplateNodeId)
            .HasColumnName("parent_template_node_id");

        builder.Property(n => n.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(n => n.NormalizedName)
            .IsRequired()
            .HasColumnName("normalized_name");

        // A plain string column: no Quran foreign key and no ayah validation exists on it (§7.4).
        builder.Property(n => n.RepresentativeQuranExcerpt)
            .HasColumnName("representative_quran_excerpt");

        builder.Property(n => n.Description)
            .HasColumnName("description");

        builder.Property(n => n.SiblingOrder)
            .IsRequired()
            .HasColumnName("sibling_order");

        builder.Property(n => n.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(n => n.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(n => n.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<DoorTemplate>()
            .WithMany()
            .HasForeignKey(n => n.DoorTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TemplateNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentTemplateNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.DoorTemplateId, n.ParentTemplateNodeId })
            .HasDatabaseName("ix_abwab_template_nodes_template_parent");

        builder.HasIndex(n => new { n.DoorTemplateId, n.NormalizedName })
            .HasDatabaseName("ix_abwab_template_nodes_normalized_name");
    }
}
