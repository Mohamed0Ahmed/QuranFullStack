namespace QuranDashboard.Domain.Linking;

public sealed class LinkingDoorAyahWord
{
    public long DoorAyahId { get; set; }

    public int QuranWordId { get; set; }

    public int AyahId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int CreatedBy { get; set; }
}
