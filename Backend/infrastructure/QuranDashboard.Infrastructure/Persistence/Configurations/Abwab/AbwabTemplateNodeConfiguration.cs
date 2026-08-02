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

        // Npgsql maps this to a real text[] column. Deliberately not the abwab_door_aliases child
        // table: template aliases are never searched, soft-deleted, or individually identified, so
        // that table's three mechanisms would have no consumer here. Writers must ASSIGN a new
        // array — EF change-tracks this by reference.
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

        // uint + IsRowVersion() maps directly to Postgres's xmin system column — no HasColumnName,
        // since giving it one would make EF treat it as a real column and add it to migrations.
        builder.Property(n => n.Version)
            .IsRowVersion();

        // Restrict on both, matching AbwabDoor: deletion here is soft, so a hard-delete cascade
        // would be wrong.
        builder.HasOne<AbwabTemplate>()
            .WithMany()
            .HasForeignKey(n => n.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabTemplateNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exactly one live root per template. Without it the mockup's own "add a root element"
        // control could produce a second root, and the apply's children-only copy would not know
        // which root's children to copy.
        builder.HasIndex(n => n.TemplateId)
            .IsUnique()
            .HasFilter("parent_node_id IS NULL AND deleted_at IS NULL");

        // The doors' unique name index, one table over. It is what confines an apply collision to
        // the root: a template can never hold two same-named siblings, so a deep copy cannot fail
        // mid-way on a constraint naming a door that does not exist yet.
        // NULLS NOT DISTINCT is load-bearing — parent_node_id is NULL for the root, and Postgres
        // NULLs never collide in a unique index by default. Requires PostgreSQL 15+ (target 16).
        builder.HasIndex(n => new { n.TemplateId, n.ParentNodeId, n.Name })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(n => new { n.TemplateId, n.ParentNodeId, n.OrderValue });
        builder.HasIndex(n => n.ParentNodeId);
        builder.HasIndex(n => n.DeletedAtUtc);
    }
}
