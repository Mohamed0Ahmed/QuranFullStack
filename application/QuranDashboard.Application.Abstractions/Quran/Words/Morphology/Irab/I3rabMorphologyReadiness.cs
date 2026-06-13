namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabMorphologyReadiness(
    bool IsReady,
    int SegmentCount,
    int ReadableWordCount,
    int NullFormCount,
    string? RefusalReason)
{
    public static I3rabMorphologyReadiness Ready(int segmentCount, int readableWordCount, int nullFormCount) =>
        new(true, segmentCount, readableWordCount, nullFormCount, null);

    public static I3rabMorphologyReadiness NotReady(
        int segmentCount,
        int readableWordCount,
        int nullFormCount,
        string refusalReason) =>
        new(false, segmentCount, readableWordCount, nullFormCount, refusalReason);
}
