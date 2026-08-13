namespace QuranDashboard.Domain.Linking;

public sealed class LinkingUnit
{
    public long Id { get; set; }

    public int DoorId { get; set; }

    public string Identity { get; set; } = string.Empty;

    public byte[] IdentityHash { get; set; } = [];

    public bool IsGrouped { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int CreatedBy { get; set; }
}
