namespace QuranDashboard.Domain.Quran.MushafPages;

public sealed class MushafPage
{
    public short PageNumber { get; set; }
    public short FirstSurahNumber { get; set; }
    public short FirstAyahNumber { get; set; }
    public short LastSurahNumber { get; set; }
    public short LastAyahNumber { get; set; }
    public short LinesCount { get; set; }
}
