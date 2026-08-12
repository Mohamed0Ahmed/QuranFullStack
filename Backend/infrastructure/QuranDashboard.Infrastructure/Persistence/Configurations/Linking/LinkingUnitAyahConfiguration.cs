using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingUnitAyahConfiguration : IEntityTypeConfiguration<LinkingUnitAyah>
{
    public void Configure(EntityTypeBuilder<LinkingUnitAyah> builder)
    {
        builder.ToTable("linking_unit_ayahs");

        builder.HasKey(unitAyah => unitAyah.Id);
        builder.Property(unitAyah => unitAyah.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(unitAyah => unitAyah.UnitId)
            .IsRequired()
            .HasColumnName("unit_id");

        builder.Property(unitAyah => unitAyah.SourceContributionId)
            .IsRequired()
            .HasColumnName("source_contribution_id");

        builder.Property(unitAyah => unitAyah.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(unitAyah => unitAyah.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.HasOne<LinkingUnit>()
            .WithMany()
            .HasForeignKey(unitAyah => new { unitAyah.UnitId, unitAyah.SourceContributionId })
            .HasPrincipalKey(unit => new { unit.Id, unit.SourceContributionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(unitAyah => unitAyah.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(unitAyah => new { unitAyah.SourceContributionId, unitAyah.AyahId })
            .IsUnique();

        builder.HasIndex(unitAyah => new { unitAyah.UnitId, unitAyah.OrderValue });

        builder.HasIndex(unitAyah => unitAyah.AyahId);
    }
}
