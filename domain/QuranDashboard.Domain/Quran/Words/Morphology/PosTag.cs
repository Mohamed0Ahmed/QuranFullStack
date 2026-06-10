namespace QuranDashboard.Domain.Quran.Words.Morphology;

public sealed class PosTag
{
    public string Code { get; set; } = string.Empty;
    public string ArabicLabel { get; set; } = string.Empty;
    public string EnglishLabel { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public short SortOrder { get; set; }
    public string? Description { get; set; }
}
