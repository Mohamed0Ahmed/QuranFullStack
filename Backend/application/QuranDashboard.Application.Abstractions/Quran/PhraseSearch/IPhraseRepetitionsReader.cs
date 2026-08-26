using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseRepetitionsReader
{
    Task<PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>> GetCapabilitiesAsync(
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseRepetitionsPageResponse>> GetRepetitionsAsync(
        PhraseTextMode mode,
        short wordCount,
        PhraseRepetitionSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseOccurrencePageResponse>> GetOccurrencesAsync(
        Guid expectedBuildId,
        long variantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
