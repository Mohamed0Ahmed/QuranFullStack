namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public sealed record PhraseIndexBuildExecution(
    Guid BuildId,
    PhraseIndexBuildOutcome Outcome,
    string Message,
    string ReportDirectory,
    PhraseIndexBuildTotals Totals,
    string SourceFingerprint,
    long SourceRevision,
    Guid? PreviousBuildId,
    Guid? ActiveBuildId)
{
    public bool Succeeded => Outcome == PhraseIndexBuildOutcome.Succeeded;
}
