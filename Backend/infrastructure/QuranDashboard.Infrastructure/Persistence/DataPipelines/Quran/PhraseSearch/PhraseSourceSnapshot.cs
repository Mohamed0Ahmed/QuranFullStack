namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceSnapshot(
    long SourceRevision,
    string StoredSourceFingerprint,
    string SourceFingerprint,
    Guid? ActiveBuildId,
    Guid? PreviousBuildId,
    IReadOnlyList<PhraseSourceToken> Tokens,
    int AyahCount,
    short MaximumAyahLength,
    IReadOnlyList<PhraseBuildCheck> Checks);
