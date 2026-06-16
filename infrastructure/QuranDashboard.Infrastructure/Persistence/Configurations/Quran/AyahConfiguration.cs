using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran;

public sealed class AyahConfiguration : IEntityTypeConfiguration<Ayah>
{
    public void Configure(EntityTypeBuilder<Ayah> builder)
    {
        builder.ToTable("quran_ayahs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(a => a.SurahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("surah_number");

        builder.Property(a => a.AyahNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("ayah_number");

        builder.Property(a => a.VerseKey)
            .IsRequired()
            .HasColumnName("verse_key");

        builder.Property(a => a.TextUthmani)
            .IsRequired()
            .HasColumnName("text_uthmani");

        builder.Property(a => a.WordsCountSource)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("words_count_source");

        builder.Property(a => a.WordsCountReal)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("words_count_real");

        builder.Property(a => a.PageFrom)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("page_from");

        builder.Property(a => a.PageTo)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("page_to");

        builder.Property(a => a.JuzNumber)
            .HasColumnType("smallint")
            .HasColumnName("juz_number");

        builder.Property(a => a.HizbNumber)
            .HasColumnType("smallint")
            .HasColumnName("hizb_number");

        builder.Property(a => a.RubNumber)
            .HasColumnType("smallint")
            .HasColumnName("rub_number");

        builder.HasIndex(a => a.VerseKey).IsUnique();
        builder.HasIndex(a => new { a.SurahNumber, a.AyahNumber }).IsUnique();
        builder.HasIndex(a => a.JuzNumber);
        builder.HasIndex(a => a.HizbNumber);
        builder.HasIndex(a => a.RubNumber);

        builder.HasOne(a => a.Surah)
            .WithMany()
            .HasForeignKey(a => a.SurahNumber);

        builder.HasOne<Juz>()
            .WithMany()
            .HasForeignKey(a => a.JuzNumber)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Hizb>()
            .WithMany()
            .HasForeignKey(a => a.HizbNumber)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rub>()
            .WithMany()
            .HasForeignKey(a => a.RubNumber)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
