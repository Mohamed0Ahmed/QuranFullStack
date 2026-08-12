namespace QuranDashboard.Domain.Linking;

public sealed class LinkingUnitAyah
{
    public long Id { get; set; }

    public long UnitId { get; set; }

    public long SourceContributionId { get; set; }

    public int AyahId { get; set; }

    public int OrderValue { get; set; }
}
