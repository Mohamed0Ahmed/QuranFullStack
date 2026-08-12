namespace QuranDashboard.Domain.Linking;

public sealed class LinkingUnit
{
    public long Id { get; set; }

    public long SourceContributionId { get; set; }

    public int OrderValue { get; set; }

    public bool IsGrouped { get; set; }
}
