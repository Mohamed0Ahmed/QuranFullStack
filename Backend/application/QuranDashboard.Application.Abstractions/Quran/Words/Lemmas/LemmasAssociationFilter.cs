namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

// A positive-but-unmatched RootId yields an empty page (200), not a 404.
public sealed record LemmasAssociationFilter(int? RootId)
{
    public static readonly LemmasAssociationFilter None = new((int?)null);

    public bool IsActive => RootId.HasValue;

    public bool IsValid => RootId is null || RootId.Value > 0;

    public static LemmasAssociationFilter FromRaw(int? rootId) => new(rootId);
}
