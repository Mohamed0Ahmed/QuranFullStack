namespace QuranDashboard.Application.Abstractions.Quran.Words;

public sealed record UniqueWordsAssociationFilter(string? PrimaryType, int? RootId)
{
    public static readonly UniqueWordsAssociationFilter None = new(null, null);

    public string? NormalizedPrimaryType =>
        string.IsNullOrWhiteSpace(PrimaryType) ? null : PrimaryType.Trim();

    public bool IsActive => NormalizedPrimaryType is not null || RootId.HasValue;

    public bool IsValid => RootId is null || RootId.Value > 0;

    public static UniqueWordsAssociationFilter FromRaw(string? primaryType, int? rootId) =>
        new(string.IsNullOrWhiteSpace(primaryType) ? null : primaryType.Trim(), rootId);
}
