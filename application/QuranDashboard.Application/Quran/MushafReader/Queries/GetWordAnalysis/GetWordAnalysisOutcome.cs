using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

public abstract record GetWordAnalysisOutcome
{
    private GetWordAnalysisOutcome() { }

    public sealed record Success(WordAnalysisResponse Response) : GetWordAnalysisOutcome;

    public sealed record InvalidWordLocation : GetWordAnalysisOutcome;

    public sealed record NotFound : GetWordAnalysisOutcome;

    public sealed record NotAnalyzable : GetWordAnalysisOutcome;

    public sealed record IncompleteData : GetWordAnalysisOutcome;
}
