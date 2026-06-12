namespace QuranDashboard.Domain.Quran.Words.Morphology.Irab;

public sealed class QuranI3rabRule
{
    public int Id { get; set; }
    public string SignatureKey { get; set; } = string.Empty;
    public string RuleFamily { get; set; } = string.Empty;
    public string I3rabArabic { get; set; } = string.Empty;
    public string DefaultStatus { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short SortOrder { get; set; }
}
