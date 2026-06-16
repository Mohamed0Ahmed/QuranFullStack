namespace QuranDashboard.Domain.Quran.Translations;

public sealed class TranslationAyahEntry
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public int AyahId { get; set; }
    public string? VerseKey { get; set; }
    public string Text { get; set; } = string.Empty;
}
