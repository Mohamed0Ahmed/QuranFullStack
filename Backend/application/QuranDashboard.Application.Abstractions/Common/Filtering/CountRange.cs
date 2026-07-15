namespace QuranDashboard.Application.Abstractions.Common.Filtering;

/// <summary>
/// An inclusive integer count-range filter (Feature 026, US5). Either bound may be
/// left open. The URL grammar is <c>min..max</c>; the same value object backs the
/// SQL predicates (Unique Words) and the in-memory predicates (Roots/Lemmas/Stems).
/// </summary>
public readonly record struct CountRange(int? Min, int? Max)
{
    /// <summary>An open range that matches every value.</summary>
    public static readonly CountRange Unbounded = new(null, null);

    /// <summary>True when at least one bound is set (the filter narrows results).</summary>
    public bool IsActive => Min.HasValue || Max.HasValue;

    /// <summary>
    /// Well-formed when both bounds are non-negative and, when both are present,
    /// <see cref="Min"/> ≤ <see cref="Max"/>. A malformed range is rejected with a
    /// controlled 400 (InvalidFilter) at the handler.
    /// </summary>
    public bool IsValid =>
        (Min is null || Min.Value >= 0)
        && (Max is null || Max.Value >= 0)
        && (Min is null || Max is null || Min.Value <= Max.Value);

    /// <summary>True when <paramref name="value"/> falls inside the (inclusive, possibly open) range.</summary>
    public bool Includes(int value) =>
        (Min is null || value >= Min.Value) && (Max is null || value <= Max.Value);
}
