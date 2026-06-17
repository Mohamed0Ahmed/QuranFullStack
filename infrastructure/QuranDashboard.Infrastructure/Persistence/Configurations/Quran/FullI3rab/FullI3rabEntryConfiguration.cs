using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.FullI3rab;

public sealed class FullI3rabEntryConfiguration : IEntityTypeConfiguration<FullI3rabEntry>
{
    public void Configure(EntityTypeBuilder<FullI3rabEntry> builder)
    {
        builder.ToTable("quran_full_i3rab_entries", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_full_i3rab_entries_covered_ayah_count",
                "covered_ayah_count >= 1");
            table.HasCheckConstraint(
                "CK_quran_full_i3rab_entries_i3rab_html",
                "i3rab_html <> ''");
            table.HasCheckConstraint(
                "CK_quran_full_i3rab_entries_source_shape",
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

        builder.Property(e => e.I3rabHtml)
            .IsRequired()
            .HasColumnName("i3rab_html");

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

        builder.HasOne<FullI3rabSource>()
            .WithMany()
            .HasForeignKey(e => e.SourceId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(e => e.LeaderAyahId);
    }
}
