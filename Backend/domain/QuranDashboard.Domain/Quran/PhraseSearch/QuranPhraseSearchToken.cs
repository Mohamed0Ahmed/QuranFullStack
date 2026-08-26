namespace QuranDashboard.Domain.Quran.PhraseSearch;

public sealed class QuranPhraseSearchToken
{
    public Guid BuildId { get; set; }
    public PhraseTextMode Mode { get; set; }
    public long Id { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public int[] ExactTokenIds { get; set; } = [];
}
