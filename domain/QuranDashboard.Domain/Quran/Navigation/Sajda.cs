namespace QuranDashboard.Domain.Quran.Navigation;

public sealed class Sajda
{
    public short SajdahNumber { get; set; }
    public int AyahId { get; set; }
    public string VerseKey { get; set; } = string.Empty;
    public SajdahType SajdahType { get; set; }
}
