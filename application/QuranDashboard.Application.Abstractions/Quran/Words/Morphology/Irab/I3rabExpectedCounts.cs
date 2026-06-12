namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

/// <summary>
/// Data-dependent counts the i‘rab generation refuses to run against unless they match
/// (FR-025), and that the post-write gate asserts. Production uses <see cref="Production"/>
/// (the locked corpus totals); tests inject the counts of their representative fixture so the
/// full pipeline can be exercised end-to-end. The catalogue-derived counts (142 rules /
/// 67 families) stay in <see cref="I3rabInvariants"/> because the catalogue is always seeded whole.
/// </summary>
public sealed record I3rabExpectedCounts(
    int SegmentCount,
    int ReadableWordCount,
    int NullFormCount)
{
    public static I3rabExpectedCounts Production { get; } = new(
        I3rabInvariants.ExpectedSegmentCount,
        I3rabInvariants.ExpectedWordCount,
        I3rabInvariants.ExpectedNullFormCount);
}
