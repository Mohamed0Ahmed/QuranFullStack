using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class CategorySearchAliasConfiguration : IEntityTypeConfiguration<CategorySearchAlias>
{
    public void Configure(EntityTypeBuilder<CategorySearchAlias> builder)
    {
        builder.ToTable("abwab_category_search_aliases");

        builder.HasKey(a => a.CategorySearchAliasId);
        builder.Property(a => a.CategorySearchAliasId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(a => a.CategoryId)
            .IsRequired()
            .HasColumnName("category_id");

        builder.Property(a => a.Value)
            .IsRequired()
            .HasColumnName("value");

        builder.Property(a => a.NormalizedValue)
            .IsRequired()
            .HasColumnName("normalized_value");

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(a => a.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(a => a.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Aliases are separately owned rows: unique only within their own category, never globally.
        builder.HasIndex(a => new { a.CategoryId, a.NormalizedValue })
            .IsUnique()
            .HasDatabaseName("ix_abwab_category_search_aliases_active")
            .HasFilter("is_deleted = false");
    }
}
