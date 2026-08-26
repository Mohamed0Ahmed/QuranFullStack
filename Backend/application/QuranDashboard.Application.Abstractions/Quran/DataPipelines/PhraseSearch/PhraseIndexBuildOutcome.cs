namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public enum PhraseIndexBuildOutcome
{
    Succeeded = 1,
    Refused = 2,
    SourceApprovalRequired = 3,
    Failed = 4,
    Cancelled = 5,
}
