namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

/// <summary>
/// Association filter for the Lemmas list (Feature 026, US7). Narrows lemmas by their owned
/// root via the real FK <c>quran_lemmas.root_id</c> — a true belonging relation, the same
/// <c>RootId</c> the list row already displays. The predicate runs in memory over the cached
/// whole-summary rows (no backend cache-key change). A positive-but-unmatched id yields an
/// empty page (200), not a 404.
/// </summary>
public sealed record LemmasAssociationFilter(int? RootId)
{
    public static readonly LemmasAssociationFilter None = new((int?)null);

    public bool IsActive => RootId.HasValue;

    public bool IsValid => RootId is null || RootId.Value > 0;

    public static LemmasAssociationFilter FromRaw(int? rootId) => new(rootId);
}
