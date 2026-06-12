namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

internal sealed record I3rabSourceSnapshot(
    int SegmentCount,
    int ReadableWordCount,
    string SegmentFingerprint,
    string QuranWordsFingerprint,
    string PosTagsFingerprint,
    IReadOnlyList<int> NullFormSegmentIds);
