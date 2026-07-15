namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// Association filter for the Unique Words list (Feature 026, US7). Narrows unique word
/// identities by their <b>primary</b> word type (POS code) and/or <b>primary</b> root —
/// the very same primary-selection rule the displayed chip uses, so the filter and the
/// displayed primary value can never disagree (the chip⇔filter invariant). Predicates run
/// in the base SQL. <see cref="RootId"/> must be positive (structural validity here);
/// <see cref="PrimaryType"/> is additionally validated against the POS catalogue in the
/// handler so an unknown code is rejected with a controlled 400.
/// </summary>
public sealed record UniqueWordsAssociationFilter(string? PrimaryType, int? RootId)
{
    public static readonly UniqueWordsAssociationFilter None = new(null, null);

    /// <summary>The trimmed, non-empty POS code (null when absent).</summary>
    public string? NormalizedPrimaryType =>
        string.IsNullOrWhiteSpace(PrimaryType) ? null : PrimaryType.Trim();

    /// <summary>True when at least one association bound narrows the result.</summary>
    public bool IsActive => NormalizedPrimaryType is not null || RootId.HasValue;

    /// <summary>
    /// Structural validity: a supplied root id must be positive. The POS code's existence in
    /// the catalogue is checked separately (async) by the handler.
    /// </summary>
    public bool IsValid => RootId is null || RootId.Value > 0;

    public static UniqueWordsAssociationFilter FromRaw(string? primaryType, int? rootId) =>
        new(string.IsNullOrWhiteSpace(primaryType) ? null : primaryType.Trim(), rootId);
}
