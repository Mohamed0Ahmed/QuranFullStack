namespace QuranDashboard.Domain.Linking;

public sealed class LinkingDoorAyah
{
    public long Id { get; set; }

    public int DoorId { get; set; }

    public int AyahId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int CreatedBy { get; set; }
}
