namespace QuranDashboard.Application.Abstractions.Quran.Words;

// Narrows unique words by their primary POS code and/or primary root using the same primary-selection
// rule as the displayed chip, so the filter and the displayed value can never disagree (chip⇔filter invariant).
public sealed record UniqueWordsAssociationFilter(string? PrimaryType, int? RootId)
{
    public static readonly UniqueWordsAssociationFilter None = new(null, null);

    public string? NormalizedPrimaryType =>
        string.IsNullOrWhiteSpace(PrimaryType) ? null : PrimaryType.Trim();

    public bool IsActive => NormalizedPrimaryType is not null || RootId.HasValue;

    // Structural check only: a supplied root id must be positive. The POS code is validated against
    // the catalogue (async) by the handler.
    public bool IsValid => RootId is null || RootId.Value > 0;

    public static UniqueWordsAssociationFilter FromRaw(string? primaryType, int? rootId) =>
        new(string.IsNullOrWhiteSpace(primaryType) ? null : primaryType.Trim(), rootId);
}
