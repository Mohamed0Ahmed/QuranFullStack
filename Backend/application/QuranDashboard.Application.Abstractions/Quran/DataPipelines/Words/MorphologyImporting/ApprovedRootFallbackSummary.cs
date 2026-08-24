namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

public sealed record ApprovedRootFallbackSummary(
    string ArtifactSha256,
    string Source,
    int ExpectedEntries,
    int AppliedEntries,
    int StrongEntries,
    int LinguisticEntries,
    int LexicalEntries);
