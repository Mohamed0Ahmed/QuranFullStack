using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Display;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Display;

public sealed class UniqueTashkeelWordConfiguration : IEntityTypeConfiguration<UniqueTashkeelWord>
{
    public void Configure(EntityTypeBuilder<UniqueTashkeelWord> builder)
    {
        builder.ToTable("quran_words_unique_tashkeel");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.TextUthmani)
            .IsRequired()
            .HasColumnName("text_uthmani");

        builder.Property(x => x.TextUthmaniSimple)
            .IsRequired()
            .HasColumnName("text_uthmani_simple");

        builder.Property(x => x.TextImlaeiSimple)
            .IsRequired()
            .HasColumnName("text_imlaei_simple");

        // canonical fold map: ArabicSearchQueryNormalizer.FoldFrom/FoldTo (decision 5 (DRY)). The literal
        // strings below are left as-is on purpose — they are part of the EF computed-column SQL captured in
        // the migration/model snapshot; redirecting them to the shared consts would not change behavior but
        // would still touch generated migration surface, which is out of scope for this refactor.
        builder.Property<string>("SearchTextNormalized")
            .HasColumnName("search_text_normalized")
            .HasComputedColumnSql("translate(lower(text_uthmani_simple || ' ' || text_imlaei_simple), 'أإآٱؤئةىي', 'ااااواهيي')", stored: true);

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

        builder.Property(x => x.FirstQuranWordId)
            .IsRequired()
            .HasColumnName("first_quran_word_id");

        builder.Property(x => x.FirstLocation)
            .IsRequired()
            .HasColumnName("first_location");

        builder.Property(x => x.FirstSurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_surah_number");

        builder.Property(x => x.FirstAyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_ayah_number");

        builder.Property(x => x.FirstWordOrderInMushaf)
            .IsRequired()
            .HasColumnName("first_word_order_in_mushaf");

        builder.Property(x => x.FirstPageNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_page_number");

        builder.Property(x => x.FirstLineNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("first_line_number");

        builder.HasIndex(x => x.TextUthmani).IsUnique();

        builder.HasIndex(x => x.FirstWordOrderInMushaf).IsUnique();

        builder.HasIndex("SearchTextNormalized")
            .HasDatabaseName("IX_quran_words_unique_tashkeel_search_text_normalized")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(x => x.FirstQuranWordId);
    }
}
