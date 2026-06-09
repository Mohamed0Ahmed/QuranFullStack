using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Display;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Display;

public sealed class OrderedSimpleWordConfiguration : IEntityTypeConfiguration<OrderedSimpleWord>
{
    public void Configure(EntityTypeBuilder<OrderedSimpleWord> builder)
    {
        builder.ToTable("quran_words_ordered_simple");

        builder.HasKey(x => x.WordOrderInMushaf);
        builder.Property(x => x.WordOrderInMushaf)
            .ValueGeneratedNever()
            .HasColumnName("word_order_in_mushaf");

        builder.Property(x => x.QuranWordId)
            .IsRequired()
            .HasColumnName("quran_word_id");

        builder.Property(x => x.Location)
            .IsRequired()
            .HasColumnName("location");

        builder.Property(x => x.VerseKey)
            .IsRequired()
            .HasColumnName("verse_key");

        builder.Property(x => x.SurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("surah_number");

        builder.Property(x => x.AyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("ayah_number");

        builder.Property(x => x.PageNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("page_number");

        builder.Property(x => x.LineNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("line_number");

        builder.Property(x => x.WordOrderInAyah)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("word_order_in_ayah");

        builder.Property(x => x.WordOrderInSurah)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("word_order_in_surah");

        builder.Property(x => x.TextUthmaniSimple)
            .IsRequired()
            .HasColumnName("text_uthmani_simple");

        builder.Property(x => x.TextImlaeiSimple)
            .IsRequired()
            .HasColumnName("text_imlaei_simple");

        builder.Property(x => x.OccurrencesCount)
            .IsRequired()
            .HasColumnName("occurrences_count");

        builder.Property(x => x.AyahsCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("ayahs_count");

        builder.Property(x => x.SurahsCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("surahs_count");

        builder.HasIndex(x => x.QuranWordId).IsUnique();

        builder.HasIndex(x => new { x.SurahNumber, x.WordOrderInSurah });

        builder.HasIndex(x => new { x.SurahNumber, x.AyahNumber, x.WordOrderInAyah });

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(x => x.QuranWordId);
    }
}
