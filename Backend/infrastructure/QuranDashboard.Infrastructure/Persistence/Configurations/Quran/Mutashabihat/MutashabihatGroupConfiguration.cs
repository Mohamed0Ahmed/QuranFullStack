using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Mutashabihat;

public sealed class MutashabihatGroupConfiguration : IEntityTypeConfiguration<MutashabihatGroup>
{
    public void Configure(EntityTypeBuilder<MutashabihatGroup> builder)
    {
        builder.ToTable("quran_mutashabihat_groups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(g => g.SourceGroupId)
            .IsRequired()
            .HasColumnName("source_group_id");

        builder.Property(g => g.RepresentativeAyahId)
            .IsRequired()
            .HasColumnName("representative_ayah_id");

        builder.Property(g => g.RepresentativeWordFrom)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("representative_word_from");

        builder.Property(g => g.RepresentativeWordTo)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("representative_word_to");

        builder.Property(g => g.OccurrenceCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("occurrence_count");

        builder.Property(g => g.DistinctAyahCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("distinct_ayah_count");

        builder.Property(g => g.DistinctSurahCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("distinct_surah_count");

        builder.Property(g => g.RawSourceCounts)
            .HasColumnType("jsonb")
            .HasColumnName("raw_source_counts");

        builder.HasIndex(g => g.SourceGroupId).IsUnique();
        builder.HasIndex(g => g.RepresentativeAyahId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(g => g.RepresentativeAyahId);
    }
}
