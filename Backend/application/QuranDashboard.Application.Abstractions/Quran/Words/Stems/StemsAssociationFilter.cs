namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

// Primary-not-sole: a stem whose primary (dominant) root/lemma differs is excluded even if it
// co-occurs with the filtered id. A positive-but-unmatched id yields an empty page (200), not a 404.
public sealed record StemsAssociationFilter(int? RootId, int? LemmaId)
{
    public static readonly StemsAssociationFilter None = new(null, null);

    public bool IsActive => RootId.HasValue || LemmaId.HasValue;

    public bool IsValid =>
        (RootId is null || RootId.Value > 0)
        && (LemmaId is null || LemmaId.Value > 0);

    public static StemsAssociationFilter FromRaw(int? rootId, int? lemmaId) => new(rootId, lemmaId);
}
