namespace QuranDashboard.Domain.Quran.Words.Display;

public sealed class OrderedTashkeelWord
{
    public int WordOrderInMushaf { get; set; }
    public int QuranWordId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string VerseKey { get; set; } = string.Empty;
    public short SurahNumber { get; set; }
    public short AyahNumber { get; set; }
    public short PageNumber { get; set; }
    public short LineNumber { get; set; }
    public short WordOrderInAyah { get; set; }
    public short WordOrderInSurah { get; set; }
    public string TextUthmani { get; set; } = string.Empty;
    public string TextUthmaniSimple { get; set; } = string.Empty;
    public string TextImlaeiSimple { get; set; } = string.Empty;
    public int OccurrencesCount { get; set; }
    public short AyahsCount { get; set; }
    public short SurahsCount { get; set; }
}
