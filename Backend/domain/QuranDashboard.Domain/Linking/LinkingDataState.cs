namespace QuranDashboard.Domain.Linking;

public sealed class LinkingDataState
{
    public short Id { get; set; }

    public long Generation { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
