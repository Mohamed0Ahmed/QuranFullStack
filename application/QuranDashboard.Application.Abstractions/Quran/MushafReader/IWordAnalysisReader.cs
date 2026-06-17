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
/// (404), or <see cref="NotAnalyzable"/> (400 — the location points at an
/// ayah-end marker rather than a readable word).
/// </summary>
public abstract record WordAnalysisOutcome
{
    private WordAnalysisOutcome() { }

    public sealed record Found(WordAnalysisResponse Response) : WordAnalysisOutcome;

    public sealed record NotFound : WordAnalysisOutcome;

    public sealed record NotAnalyzable : WordAnalysisOutcome;
}
