using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranDashboard.Domain.Quran.Surahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran;

public sealed class SurahConfiguration : IEntityTypeConfiguration<Surah>
{
    public void Configure(EntityTypeBuilder<Surah> builder)
    {
        builder.ToTable("quran_surahs");

        builder.HasKey(s => s.SurahNumber);
        builder.Property(s => s.SurahNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("surah_number");

        builder.Property(s => s.NameArabic)
            .IsRequired()
            .HasColumnName("name_arabic");

        builder.Property(s => s.NameSimple)
            .IsRequired()
            .HasColumnName("name_simple");

        builder.Property(s => s.NameTransliteration)
            .IsRequired()
            .HasColumnName("name_transliteration");

        builder.Property(s => s.RevelationPlace)
            .HasConversion(
                value => ToDatabaseValue(value),
                value => ToRevelationPlace(value))
            .IsRequired()
            .HasColumnName("revelation_place");

        builder.Property(s => s.RevelationOrder)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("revelation_order");

        builder.Property(s => s.VersesCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("verses_count");

        builder.Property(s => s.BismillahPre)
            .IsRequired()
            .HasColumnName("bismillah_pre");

        builder.HasIndex(s => s.NameArabic).IsUnique();
    }

    private static string ToDatabaseValue(RevelationPlace value) => value switch
    {
        RevelationPlace.Makkah => "makkah",
        RevelationPlace.Madinah => "madinah",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown revelation place.")
    };

    private static RevelationPlace ToRevelationPlace(string value) => value switch
    {
        "makkah" => RevelationPlace.Makkah,
        "madinah" => RevelationPlace.Madinah,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown revelation place value.")
    };
}
