namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedAffectedContribution
{
    public Guid PreflightId { get; set; }
    public long ContributionId { get; set; }
    public uint ExpectedContributionVersion { get; set; }
}
