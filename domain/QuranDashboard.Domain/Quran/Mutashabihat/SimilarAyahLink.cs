namespace QuranDashboard.Domain.Quran.Mutashabihat;

public sealed class SimilarAyahLink
{
    public int Id { get; set; }
    public int SourceAyahId { get; set; }
    public int TargetAyahId { get; set; }
    public short Score { get; set; }
    public short Coverage { get; set; }
    public short MatchedWordsCount { get; set; }
    public string MatchWords { get; set; } = string.Empty;
}
