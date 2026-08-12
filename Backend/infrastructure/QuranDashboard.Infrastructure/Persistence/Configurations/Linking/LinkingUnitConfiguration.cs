using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingUnitConfiguration : IEntityTypeConfiguration<LinkingUnit>
{
    public void Configure(EntityTypeBuilder<LinkingUnit> builder)
    {
        builder.ToTable("linking_units");

        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(unit => unit.SourceContributionId)
            .IsRequired()
            .HasColumnName("source_contribution_id");

        builder.Property(unit => unit.OrderValue)
            .IsRequired()
            .HasColumnName("order_value");

        builder.Property(unit => unit.IsGrouped)
            .IsRequired()
            .HasColumnName("is_grouped");

        builder.HasAlternateKey(unit => new { unit.Id, unit.SourceContributionId });

        builder.HasOne<LinkingSourceContribution>()
            .WithMany()
            .HasForeignKey(unit => unit.SourceContributionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(unit => new { unit.SourceContributionId, unit.OrderValue })
            .IsUnique();
    }
}
