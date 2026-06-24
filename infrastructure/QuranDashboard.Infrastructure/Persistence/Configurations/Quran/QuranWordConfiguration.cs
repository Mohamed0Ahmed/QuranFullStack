
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran;

public sealed class QuranWordConfiguration : IEntityTypeConfiguration<QuranWord>
{
    public void Configure(EntityTypeBuilder<QuranWord> builder)
    {
        builder.ToTable("quran_words");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(w => w.Location)
            .IsRequired()
            .HasColumnName("location");

        builder.Property(w => w.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(w => w.SurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("surah_number");

        builder.Property(w => w.AyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("ayah_number");

        builder.Property(w => w.WordNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("word_number");

        builder.Property(w => w.PageNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("page_number");

        builder.Property(w => w.LineNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("line_number");

        builder.Property(w => w.LineWordOrder)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("line_word_order");

        builder.Property(w => w.QpcGlyph)
            .IsRequired()
            .HasColumnName("qpc_glyph");

        builder.Property(w => w.TextUthmani)
            .IsRequired()
            .HasColumnName("text_uthmani");

        builder.Property(w => w.TextUthmaniSimple)
            .IsRequired()
            .HasColumnName("text_uthmani_simple");

        builder.Property(w => w.TextImlaeiSimple)
            .IsRequired()
            .HasColumnName("text_imlaei_simple");

        builder.Property(w => w.WordKeyImlaeiSimple)
            .IsRequired()
            .HasColumnName("word_key_imlaei_simple");

        builder.Property(w => w.IsAyahMarker)
            .IsRequired()
            .HasColumnName("is_ayah_marker");

        builder.Property(w => w.UniqueTashkeelWordId)
            .HasColumnName("unique_tashkeel_word_id");

        builder.Property(w => w.UniqueSimpleWordId)
            .HasColumnName("unique_simple_word_id");

        builder.HasIndex(w => w.Location).IsUnique();

        builder.HasIndex(w => new { w.SurahNumber, w.AyahNumber, w.WordNumber },
                "IX_quran_words_surah_ayah_word")
            .HasDatabaseName("IX_quran_words_surah_ayah_word");

        builder.HasIndex(w => new { w.PageNumber, w.LineNumber, w.LineWordOrder });

        builder.HasIndex(w => new { w.SurahNumber, w.AyahNumber, w.WordNumber },
                "IX_quran_words_readable_surah_ayah_word")
            .HasDatabaseName("IX_quran_words_readable_surah_ayah_word")
            .HasFilter("is_ayah_marker = false");

        builder.HasIndex(w => w.WordKeyImlaeiSimple, "IX_quran_words_word_key_imlaei_simple")
            .HasDatabaseName("IX_quran_words_word_key_imlaei_simple")
            .HasFilter("is_ayah_marker = false");

        builder.HasIndex(w => w.UniqueTashkeelWordId)
            .HasDatabaseName("IX_quran_words_unique_tashkeel_word_id")
            .HasFilter("is_ayah_marker = false AND unique_tashkeel_word_id IS NOT NULL");

        builder.HasIndex(w => w.UniqueSimpleWordId)
            .HasDatabaseName("IX_quran_words_unique_simple_word_id")
            .HasFilter("is_ayah_marker = false AND unique_simple_word_id IS NOT NULL");

        builder.HasOne(w => w.Ayah)
            .WithMany()
            .HasForeignKey(w => w.AyahId);

        builder.HasOne(w => w.Page)
            .WithMany()
            .HasForeignKey(w => w.PageNumber);
    }
}
