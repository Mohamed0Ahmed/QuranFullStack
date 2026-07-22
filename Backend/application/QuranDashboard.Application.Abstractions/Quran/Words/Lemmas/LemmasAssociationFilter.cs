namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

public sealed record LemmasAssociationFilter(int? RootId)
{
    public static readonly LemmasAssociationFilter None = new((int?)null);

    public bool IsActive => RootId.HasValue;

    public bool IsValid => RootId is null || RootId.Value > 0;

    public static LemmasAssociationFilter FromRaw(int? rootId) => new(rootId);
}
