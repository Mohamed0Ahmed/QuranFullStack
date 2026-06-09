namespace QuranDashboard.Domain.Quran.Words.Display;

public sealed class UniqueSimpleWord
{
    public int Id { get; set; }
    public string TextUthmaniSimple { get; set; } = string.Empty;
    public string TextImlaeiSimple { get; set; } = string.Empty;
    public int OccurrencesCount { get; set; }
    public short AyahsCount { get; set; }
    public short SurahsCount { get; set; }
    public int FirstQuranWordId { get; set; }
    public string FirstLocation { get; set; } = string.Empty;
    public short FirstSurahNumber { get; set; }
    public short FirstAyahNumber { get; set; }
    public int FirstWordOrderInMushaf { get; set; }
    public short FirstPageNumber { get; set; }
    public short FirstLineNumber { get; set; }
}
