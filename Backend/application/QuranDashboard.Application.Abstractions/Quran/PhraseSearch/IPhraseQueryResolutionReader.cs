using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseQueryResolutionReader
{
    Task<PhraseSearchReadResult<PhraseQueryResolutionResponse>> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken);
}
