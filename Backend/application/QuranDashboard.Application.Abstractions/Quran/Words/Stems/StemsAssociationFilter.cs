namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

public sealed record StemsAssociationFilter(int? RootId, int? LemmaId)
{
    public static readonly StemsAssociationFilter None = new(null, null);

    public bool IsActive => RootId.HasValue || LemmaId.HasValue;

    public bool IsValid =>
        (RootId is null || RootId.Value > 0)
        && (LemmaId is null || LemmaId.Value > 0);

    public static StemsAssociationFilter FromRaw(int? rootId, int? lemmaId) => new(rootId, lemmaId);
}
