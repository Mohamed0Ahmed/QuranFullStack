namespace QuranDashboard.Domain.Quran.Surahs;

public sealed class Surah
{
    public short SurahNumber { get; set; }
    public string NameArabic { get; set; } = string.Empty;
    public string NameSimple { get; set; } = string.Empty;
    public string NameTransliteration { get; set; } = string.Empty;
    public RevelationPlace RevelationPlace { get; set; }
    public short RevelationOrder { get; set; }
    public short VersesCount { get; set; }
    public bool BismillahPre { get; set; }
}
