using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran;

public sealed class MushafPageConfiguration : IEntityTypeConfiguration<MushafPage>
{
    public void Configure(EntityTypeBuilder<MushafPage> builder)
    {
        builder.ToTable("quran_mushaf_pages");

        builder.HasKey(p => p.PageNumber);
        builder.Property(p => p.PageNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("page_number");

        builder.Property(p => p.FirstSurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_surah_number");

        builder.Property(p => p.FirstAyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_ayah_number");

        builder.Property(p => p.LastSurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("last_surah_number");

        builder.Property(p => p.LastAyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("last_ayah_number");

        builder.Property(p => p.LinesCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("lines_count");
    }
}
