using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

/// <summary>
/// Discriminated handler outcome so the controller maps to 200 / 400 / 404
/// without exceptions for expected cases.
/// </summary>
public abstract record GetWordAnalysisOutcome
{
    private GetWordAnalysisOutcome() { }

    public sealed record Success(WordAnalysisResponse Response) : GetWordAnalysisOutcome;

    public sealed record InvalidWordLocation : GetWordAnalysisOutcome;

    public sealed record NotFound : GetWordAnalysisOutcome;

    public sealed record NotAnalyzable : GetWordAnalysisOutcome;

    public sealed record IncompleteData : GetWordAnalysisOutcome;
}
