using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Tafsirs;

public sealed class TafsirAyahEntryConfiguration : IEntityTypeConfiguration<TafsirAyahEntry>
{
    public void Configure(EntityTypeBuilder<TafsirAyahEntry> builder)
    {
        builder.ToTable("quran_tafsir_ayah_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_tafsir_ayah_entries_source_value_kind",
                "source_value_kind IN ('leader', 'member_pointer', 'flat')");
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

        builder.Property(e => e.TafsirEntryId)
            .IsRequired()
            .HasColumnName("tafsir_entry_id");

        builder.Property(e => e.VerseKey)
            .IsRequired()
            .HasColumnName("verse_key");

        builder.Property(e => e.SourceValueKind)
            .IsRequired()
            .HasColumnName("source_value_kind");

        builder.Property(e => e.SourceLeaderVerseKey)
            .IsRequired()
            .HasColumnName("source_leader_verse_key");

        builder.Property(e => e.IsGroupLeader)
            .IsRequired()
            .HasColumnName("is_group_leader");

        builder.Property(e => e.SortOrder)
            .IsRequired()
            .HasColumnName("sort_order");

        builder.HasIndex(e => new { e.SourceId, e.AyahId }).IsUnique();
        builder.HasIndex(e => new { e.SourceId, e.VerseKey }).IsUnique();
        builder.HasIndex(e => new { e.AyahId, e.SourceId });
        builder.HasIndex(e => e.TafsirEntryId);

        builder.HasOne<TafsirSource>()
            .WithMany()
            .HasForeignKey(e => e.SourceId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(e => e.AyahId);

        builder.HasOne<TafsirEntry>()
            .WithMany()
            .HasForeignKey(e => e.TafsirEntryId);
    }
}
