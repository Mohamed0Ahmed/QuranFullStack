using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.FullI3rab;

public sealed class FullI3rabAyahEntryConfiguration : IEntityTypeConfiguration<FullI3rabAyahEntry>
{
    public void Configure(EntityTypeBuilder<FullI3rabAyahEntry> builder)
    {
        builder.ToTable("quran_full_i3rab_ayah_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_full_i3rab_ayah_entries_source_value_kind",
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

        builder.Property(e => e.EntryId)
            .IsRequired()
            .HasColumnName("entry_id");

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
        builder.HasIndex(e => e.EntryId);

        builder.HasOne<FullI3rabSource>()
            .WithMany()
            .HasForeignKey(e => e.SourceId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(e => e.AyahId);

        builder.HasOne<FullI3rabEntry>()
            .WithMany()
            .HasForeignKey(e => e.EntryId);
    }
}
