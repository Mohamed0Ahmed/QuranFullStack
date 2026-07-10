namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

internal sealed record I3rabSourceSnapshot(
    int SegmentCount,
    int ReadableWordCount,
    string SegmentFingerprint,
    string QuranWordsFingerprint,
    string PosTagsFingerprint,
    IReadOnlyList<int> NullFormSegmentIds);
