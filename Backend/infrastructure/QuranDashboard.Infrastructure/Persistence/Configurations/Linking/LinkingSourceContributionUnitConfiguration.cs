using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingSourceContributionUnitConfiguration : IEntityTypeConfiguration<LinkingSourceContributionUnit>
{
    public void Configure(EntityTypeBuilder<LinkingSourceContributionUnit> builder)
    {
        builder.ToTable("linking_source_contribution_units");

        builder.HasKey(link => new { link.SourceContributionId, link.UnitId });

        builder.Property(link => link.SourceContributionId)
            .HasColumnName("source_contribution_id");

        builder.Property(link => link.UnitId)
            .HasColumnName("unit_id");

        builder.Property(link => link.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.HasOne<LinkingSourceContribution>()
            .WithMany()
            .HasForeignKey(link => link.SourceContributionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LinkingUnit>()
            .WithMany()
            .HasForeignKey(link => link.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => new { link.SourceContributionId, link.OrderValue })
            .IsUnique();

        builder.HasIndex(link => link.UnitId);
    }
}
