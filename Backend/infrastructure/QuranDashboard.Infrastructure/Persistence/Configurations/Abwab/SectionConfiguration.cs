using QuranDashboard.Domain.Abwab.Sections;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public static readonly Guid PermanentDefaultSectionId = new("00000000-0000-0000-0000-000000000001");

    public const string PermanentDefaultSectionName = "أبواب غير مصنفة";

    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("abwab_sections");

        builder.HasKey(s => s.SectionId);
        builder.Property(s => s.SectionId)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(s => s.NormalizedName)
            .IsRequired()
            .HasColumnName("normalized_name");

        builder.Property(s => s.SortOrder)
            .IsRequired()
            .HasColumnName("sort_order");

        builder.Property(s => s.IsPermanentDefault)
            .IsRequired()
            .HasColumnName("is_permanent_default");

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(s => s.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(s => s.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(s => s.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ix_abwab_sections_normalized_name_active")
            .HasFilter("is_deleted = false");

        builder.HasIndex(s => s.IsPermanentDefault)
            .IsUnique()
            .HasDatabaseName("ix_abwab_sections_permanent_default")
            .HasFilter("is_permanent_default = true AND is_deleted = false");

        builder.HasData(new Section
        {
            SectionId = PermanentDefaultSectionId,
            Name = PermanentDefaultSectionName,
            NormalizedName = ArabicNameNormalizer.Normalize(PermanentDefaultSectionName),
            SortOrder = 0,
            IsPermanentDefault = true,
            IsDeleted = false,
        });
    }
}
