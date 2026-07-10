namespace QuranDashboard.Domain.Quran.MushafPages;

public sealed class MushafLine
{
    public int Id { get; set; }
    public short PageNumber { get; set; }
    public short LineNumber { get; set; }
    public MushafLineType LineType { get; set; }
    public bool IsCentered { get; set; }
    public short? SurahNumber { get; set; }
    public int? FirstWordId { get; set; }
    public int? LastWordId { get; set; }
    public short WordsCount { get; set; }

    public MushafPage Page { get; set; } = null!;
    public Surahs.Surah? Surah { get; set; }
    public Words.QuranWord? FirstWord { get; set; }
    public Words.QuranWord? LastWord { get; set; }
}
