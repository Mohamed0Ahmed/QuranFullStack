namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

/// <summary>
/// Association filter for the Stems list (Feature 026, US7). Narrows stems by their
/// <b>primary</b> (dominant) root and/or <b>primary</b> lemma association — the same
/// <c>DominantRootId</c>/<c>DominantLemmaId</c> the list row already displays. This is a
/// <b>primary-not-sole</b> relation: a stem whose primary root/lemma differs is excluded even
/// if it co-occurs with the filtered id (documented in the reads README, pinned by tests). The
/// predicate runs in memory over the cached whole-summary rows (no backend cache-key change).
/// A positive-but-unmatched id yields an empty page (200), not a 404.
/// </summary>
public sealed record StemsAssociationFilter(int? RootId, int? LemmaId)
{
    public static readonly StemsAssociationFilter None = new(null, null);

    public bool IsActive => RootId.HasValue || LemmaId.HasValue;

    public bool IsValid =>
        (RootId is null || RootId.Value > 0)
        && (LemmaId is null || LemmaId.Value > 0);

    public static StemsAssociationFilter FromRaw(int? rootId, int? lemmaId) => new(rootId, lemmaId);
}
