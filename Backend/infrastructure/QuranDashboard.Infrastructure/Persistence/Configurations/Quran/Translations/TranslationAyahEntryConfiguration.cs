using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Translations;

public sealed class TranslationAyahEntryConfiguration : IEntityTypeConfiguration<TranslationAyahEntry>
{
    public void Configure(EntityTypeBuilder<TranslationAyahEntry> builder)
    {
        builder.ToTable("quran_translation_ayah_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_translation_ayah_entries_text",
                "text <> ''");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(e => e.SourceId)
            .IsRequired()
            .HasColumnName("source_id");

        builder.Property(e => e.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(e => e.VerseKey)
            .HasColumnName("verse_key");

        builder.Property(e => e.Text)
            .IsRequired()
            .HasColumnName("text");

        builder.HasIndex(e => new { e.SourceId, e.AyahId }).IsUnique();
        builder.HasIndex(e => new { e.AyahId, e.SourceId });

        builder.HasOne<TranslationSource>()
            .WithMany()
            .HasForeignKey(e => e.SourceId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(e => e.AyahId);
    }
}
