using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IWordAnalysisReader
{
    Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct);
}

public abstract record WordAnalysisOutcome
{
    private WordAnalysisOutcome() { }

    public sealed record Found(WordAnalysisResponse Response) : WordAnalysisOutcome;

    public sealed record NotFound : WordAnalysisOutcome;

    public sealed record NotAnalyzable : WordAnalysisOutcome;

    public sealed record IncompleteData : WordAnalysisOutcome;
}
