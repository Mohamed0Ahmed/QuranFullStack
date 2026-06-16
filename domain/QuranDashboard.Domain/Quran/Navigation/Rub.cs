namespace QuranDashboard.Domain.Quran.Navigation;

public sealed class Rub
{
    public short RubNumber { get; set; }
    public short HizbNumber { get; set; }
    public short VersesCount { get; set; }
    public int FirstAyahId { get; set; }
    public int LastAyahId { get; set; }
    public string FirstVerseKey { get; set; } = string.Empty;
    public string LastVerseKey { get; set; } = string.Empty;
}
