using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Sections;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("abwab_categories");

        builder.HasKey(c => c.CategoryId);
        builder.Property(c => c.CategoryId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(c => c.NormalizedName)
            .IsRequired()
            .HasColumnName("normalized_name");

        builder.Property(c => c.RepresentativeQuranExcerpt)
            .HasColumnName("representative_quran_excerpt");

        builder.Property(c => c.Description)
            .HasColumnName("description");

        builder.Property(c => c.ParentCategoryId)
            .HasColumnName("parent_category_id");

        builder.Property(c => c.SectionId)
            .HasColumnName("section_id");

        builder.Property(c => c.SiblingOrder)
            .HasColumnName("sibling_order");

        builder.Property(c => c.SectionOrder)
            .HasColumnName("section_order");

        builder.Property(c => c.GlobalOrder)
            .HasColumnName("global_order");

        builder.Property(c => c.AncestorIds)
            .IsRequired()
            .HasColumnName("ancestor_ids");

        builder.Property(c => c.Depth)
            .IsRequired()
            .HasColumnName("depth");

        builder.Property(c => c.OrdinaryProtectionActorSubject)
            .HasColumnName("ordinary_protection_actor_subject");

        builder.Property(c => c.OrdinaryProtectionLastEditedAtUtc)
            .HasColumnName("ordinary_protection_last_edited_at");

        builder.Property(c => c.CategoryContentRevision)
            .IsRequired()
            .HasColumnName("category_content_revision");

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(c => c.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(c => c.DeletionOperationId)
            .HasColumnName("deletion_operation_id");

        builder.HasIndex(c => c.DeletionOperationId)
            .HasDatabaseName("ix_abwab_categories_deletion_operation_id");

        builder.Property(c => c.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<Section>()
            .WithMany()
            .HasForeignKey(c => c.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Roots share one global normalized-name scope across every section (§5.1).
        builder.HasIndex(c => c.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ix_abwab_categories_root_normalized_name_active")
            .HasFilter("is_deleted = false AND parent_category_id IS NULL");

        builder.HasIndex(c => new { c.ParentCategoryId, c.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ix_abwab_categories_sibling_normalized_name_active")
            .HasFilter("is_deleted = false AND parent_category_id IS NOT NULL");

        builder.HasIndex(c => c.SectionId)
            .HasDatabaseName("ix_abwab_categories_section_id");

        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("ix_abwab_categories_parent_category_id");
    }
}
