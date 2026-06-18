using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Reads one word's analysis (occurrence, identity counts, morphology, ordered
/// color-linked segments). Returns a discriminated outcome so the controller
/// maps to 200 / 404 / 400-without-exceptions for expected cases.
/// </summary>
public interface IWordAnalysisReader
{
    Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct);
}

/// <summary>
/// Result type for word analysis: <see cref="Found"/> (200), <see cref="NotFound"/>
/// (404), <see cref="NotAnalyzable"/> (400 — ayah-end marker), or
/// <see cref="IncompleteData"/> (404 — readable word exists but required
/// morphology/identity/segment rows are missing).
/// </summary>
public abstract record WordAnalysisOutcome
{
    private WordAnalysisOutcome() { }

    public sealed record Found(WordAnalysisResponse Response) : WordAnalysisOutcome;

    public sealed record NotFound : WordAnalysisOutcome;

    public sealed record NotAnalyzable : WordAnalysisOutcome;

    public sealed record IncompleteData : WordAnalysisOutcome;
}
