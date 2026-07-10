namespace QuranDashboard.Domain.Quran.Ayahs;

public sealed class Ayah
{
    public int Id { get; set; }
    public short SurahNumber { get; set; }
    public short AyahNumber { get; set; }
    public string VerseKey { get; set; } = string.Empty;
    public string TextUthmani { get; set; } = string.Empty;
    public short WordsCountSource { get; set; }
    public short WordsCountReal { get; set; }
    public short PageFrom { get; set; }
    public short PageTo { get; set; }
    public short? JuzNumber { get; set; }
    public short? HizbNumber { get; set; }
    public short? RubNumber { get; set; }

    public Surahs.Surah Surah { get; set; } = null!;
}
