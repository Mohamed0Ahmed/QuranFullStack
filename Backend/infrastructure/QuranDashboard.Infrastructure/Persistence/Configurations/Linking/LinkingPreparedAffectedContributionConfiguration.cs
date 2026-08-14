using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingPreparedAffectedContributionConfiguration
    : IEntityTypeConfiguration<LinkingPreparedAffectedContribution>
{
    public void Configure(EntityTypeBuilder<LinkingPreparedAffectedContribution> builder)
    {
        builder.ToTable("linking_prepared_affected_contributions");

        builder.HasKey(contribution => new { contribution.PreflightId, contribution.ContributionId });
        builder.Property(contribution => contribution.PreflightId).IsRequired().HasColumnName("preflight_id");
        builder.Property(contribution => contribution.ContributionId).IsRequired().HasColumnName("contribution_id");
        builder.Property(contribution => contribution.ExpectedContributionVersion)
            .IsRequired()
            .HasColumnName("expected_contribution_version");

        builder.HasOne<LinkingPreparedPreflight>()
            .WithMany()
            .HasForeignKey(contribution => contribution.PreflightId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
