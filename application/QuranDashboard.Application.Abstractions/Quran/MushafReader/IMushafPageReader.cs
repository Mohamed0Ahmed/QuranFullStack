using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Reads a single Mushaf page (lines, words, page navigation context, and
/// division/sajda markers placed by the first-line rule).
/// </summary>
public interface IMushafPageReader
{
    /// <param name="pageNumber">Already validated to be in [1,604] by the handler.</param>
    /// <returns>The page read model, or <c>null</c> if the page has no rows.</returns>
    Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct);
}
