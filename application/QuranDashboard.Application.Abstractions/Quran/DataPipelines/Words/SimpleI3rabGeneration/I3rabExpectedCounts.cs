namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

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
