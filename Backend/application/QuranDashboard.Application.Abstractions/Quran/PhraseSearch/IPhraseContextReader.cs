using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public interface IPhraseContextReader
{
    Task<PhraseSearchReadResult<PhraseContextBranchesResponse>> GetBranchesAsync(
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging,
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseContextGroupsResponse>> GetGroupsAsync(
        PhraseContextSelection selection,
        PhraseCursorPage paging,
        CancellationToken cancellationToken);

    Task<PhraseSearchReadResult<PhraseContextOccurrencesResponse>> GetOccurrencesAsync(
        PhraseFullContextReference context,
        PhraseCursorPage paging,
        CancellationToken cancellationToken);
}
