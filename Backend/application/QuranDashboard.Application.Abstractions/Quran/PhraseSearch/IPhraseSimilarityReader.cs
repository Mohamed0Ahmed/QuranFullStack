using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseSimilarityReader
{
    Task<PhraseSearchReadResult<PhraseSimilaritySearchResponse>> SearchAsync(
        PhraseResolutionReference resolution,
        short minimumMatchedWords,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseSimilarityGroupsResponse>> GetGroupsAsync(
        PhraseTextMode mode,
        short wordCount,
        short threshold,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseSimilarityMatchesResponse>> GetMatchesAsync(
        Guid expectedBuildId,
        long anchorVariantId,
        short threshold,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
