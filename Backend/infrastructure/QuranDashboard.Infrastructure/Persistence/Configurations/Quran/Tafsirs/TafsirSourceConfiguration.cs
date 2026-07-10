using QuranDashboard.Domain.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Tafsirs;

public sealed class TafsirSourceConfiguration : IEntityTypeConfiguration<TafsirSource>
{
    public void Configure(EntityTypeBuilder<TafsirSource> builder)
    {
        builder.ToTable("quran_tafsir_sources", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_tafsir_sources_resource_kind",
                "resource_kind = 'tafsir'");
            table.HasCheckConstraint(
                "CK_quran_tafsir_sources_content_coverage_count",
                "content_coverage_count = 6236");
            table.HasCheckConstraint(
                "CK_quran_tafsir_sources_direction",
                "direction IN ('rtl', 'ltr')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.SourceKey)
            .IsRequired()
            .HasColumnName("source_key");

        builder.Property(s => s.LanguageCode)
            .IsRequired()
            .HasColumnName("language_code");

        builder.Property(s => s.LanguageNameAr)
            .IsRequired()
            .HasColumnName("language_name_ar");

        builder.Property(s => s.LanguageNameEn)
            .IsRequired()
            .HasColumnName("language_name_en");

        builder.Property(s => s.Direction)
            .IsRequired()
            .HasColumnName("direction");

        builder.Property(s => s.DisplayNameAr)
            .IsRequired()
            .HasColumnName("display_name_ar");

        builder.Property(s => s.ShortNameAr)
            .IsRequired()
            .HasColumnName("short_name_ar");

        builder.Property(s => s.DisplayNameEn)
            .IsRequired()
            .HasColumnName("display_name_en");

        builder.Property(s => s.ShortNameEn)
            .IsRequired()
            .HasColumnName("short_name_en");

        builder.Property(s => s.ContributorKey)
            .HasColumnName("contributor_key");

        builder.Property(s => s.ContributorNameAr)
            .HasColumnName("contributor_name_ar");

        builder.Property(s => s.ContributorNameEn)
            .HasColumnName("contributor_name_en");

        builder.Property(s => s.ContributorType)
            .IsRequired()
            .HasColumnName("contributor_type");

        builder.Property(s => s.ResourceKind)
            .IsRequired()
            .HasColumnName("resource_kind");

        builder.Property(s => s.TafsirKind)
            .IsRequired()
            .HasColumnName("tafsir_kind");

        builder.Property(s => s.ContentCoverageCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("content_coverage_count");

        builder.Property(s => s.PackageFile)
            .IsRequired()
            .HasColumnName("package_file");

        builder.Property(s => s.SourceFileOriginal)
            .IsRequired()
            .HasColumnName("source_file_original");

        builder.Property(s => s.Sha256)
            .IsRequired()
            .HasColumnName("sha256");

        builder.Property(s => s.FileSizeBytes)
            .IsRequired()
            .HasColumnName("file_size_bytes");

        builder.Property(s => s.LicenseStatus)
            .IsRequired()
            .HasColumnName("license_status");

        builder.Property(s => s.ProvenanceStatus)
            .IsRequired()
            .HasColumnName("provenance_status");

        builder.Property(s => s.ManifestMetadata)
            .HasColumnType("jsonb")
            .HasColumnName("manifest_metadata");

        builder.Property(s => s.ImportedAtUtc)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasColumnName("imported_at_utc");

        builder.HasIndex(s => s.SourceKey).IsUnique();
        builder.HasIndex(s => s.PackageFile).IsUnique();
        builder.HasIndex(s => s.LanguageCode);
        builder.HasIndex(s => new { s.LanguageCode, s.TafsirKind });
    }
}
