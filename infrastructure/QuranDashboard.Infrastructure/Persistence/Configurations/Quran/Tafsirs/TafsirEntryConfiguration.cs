using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Tafsirs;

public sealed class TafsirEntryConfiguration : IEntityTypeConfiguration<TafsirEntry>
{
    public void Configure(EntityTypeBuilder<TafsirEntry> builder)
    {
        builder.ToTable("quran_tafsir_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_tafsir_entries_covered_ayah_count",
                "covered_ayah_count >= 1");
            table.HasCheckConstraint(
                "CK_quran_tafsir_entries_tafsir_text",
                "tafsir_text <> ''");
            table.HasCheckConstraint(
                "CK_quran_tafsir_entries_source_shape",
                "source_shape IN ('grouped_leader', 'flat')");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(e => e.SourceId)
            .IsRequired()
            .HasColumnName("source_id");

        builder.Property(e => e.SourceEntryKey)
            .IsRequired()
            .HasColumnName("source_entry_key");

        builder.Property(e => e.LeaderAyahId)
            .IsRequired()
            .HasColumnName("leader_ayah_id");

        builder.Property(e => e.TafsirText)
            .IsRequired()
            .HasColumnName("tafsir_text");

        builder.Property(e => e.CoveredAyahCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("covered_ayah_count");

        builder.Property(e => e.CoveredAyahKeys)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("covered_ayah_keys");

        builder.Property(e => e.SourceShape)
            .IsRequired()
            .HasColumnName("source_shape");

        builder.Property(e => e.TextHash)
            .IsRequired()
            .HasColumnName("text_hash");

        builder.HasIndex(e => new { e.SourceId, e.SourceEntryKey }).IsUnique();
        builder.HasIndex(e => e.LeaderAyahId);
        builder.HasIndex(e => new { e.SourceId, e.LeaderAyahId });

        builder.HasOne<TafsirSource>()
            .WithMany()
            .HasForeignKey(e => e.SourceId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(e => e.LeaderAyahId);
    }
}
