using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class CategoryRelationshipConfiguration : IEntityTypeConfiguration<CategoryRelationship>
{
    public void Configure(EntityTypeBuilder<CategoryRelationship> builder)
    {
        builder.ToTable("abwab_category_relationships", table =>
        {
            // The shape is bound to the TYPE, not merely mutually exclusive: without the
            // relationship_type clause a Similar row could be stored in the directional columns, and
            // it would then join the Broader/Narrower graph that cycle validation walks.
            table.HasCheckConstraint(
                "ck_abwab_category_relationships_one_shape",
                $"(relationship_type <> {(int)RelationshipType.BroaderNarrower} AND lower_category_id IS NOT NULL AND higher_category_id IS NOT NULL AND source_category_id IS NULL AND target_category_id IS NULL) "
                + $"OR (relationship_type = {(int)RelationshipType.BroaderNarrower} AND lower_category_id IS NULL AND higher_category_id IS NULL AND source_category_id IS NOT NULL AND target_category_id IS NOT NULL)");

            table.HasCheckConstraint(
                "ck_abwab_category_relationships_canonical_order",
                "lower_category_id IS NULL OR lower_category_id < higher_category_id");

            table.HasCheckConstraint(
                "ck_abwab_category_relationships_no_self_link",
                "(lower_category_id IS NULL OR lower_category_id <> higher_category_id) "
                + "AND (source_category_id IS NULL OR source_category_id <> target_category_id)");
        });

        builder.HasKey(r => r.CategoryRelationshipId);
        builder.Property(r => r.CategoryRelationshipId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(r => r.RelationshipType)
            .IsRequired()
            .HasColumnName("relationship_type");

        builder.Property(r => r.LowerCategoryId)
            .HasColumnName("lower_category_id");

        builder.Property(r => r.HigherCategoryId)
            .HasColumnName("higher_category_id");

        builder.Property(r => r.SourceCategoryId)
            .HasColumnName("source_category_id");

        builder.Property(r => r.TargetCategoryId)
            .HasColumnName("target_category_id");

        builder.Property(r => r.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(r => r.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(r => r.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // RESTRICT on every endpoint: a category subtree delete must leave relationship rows present
        // and merely dormant, so no cascade path from a category to a relationship row may exist.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(r => r.LowerCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(r => r.HigherCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(r => r.SourceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(r => r.TargetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.LowerCategoryId, r.HigherCategoryId, r.RelationshipType })
            .IsUnique()
            .HasDatabaseName("ix_abwab_category_relationships_mutual_active")
            .HasFilter("is_deleted = false AND lower_category_id IS NOT NULL");

        builder.HasIndex(r => new { r.SourceCategoryId, r.TargetCategoryId })
            .IsUnique()
            .HasDatabaseName("ix_abwab_category_relationships_directional_active")
            .HasFilter($"is_deleted = false AND relationship_type = {(int)RelationshipType.BroaderNarrower}");
    }
}
