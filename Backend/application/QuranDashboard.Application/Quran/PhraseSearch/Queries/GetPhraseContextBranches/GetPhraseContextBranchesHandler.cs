using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextBranches;

public sealed class GetPhraseContextBranchesHandler(
    IPhraseContextReader reader,
    IPhraseSearchReferenceCodec codec,
    PhraseContextRequestParser parser)
{
    public async Task<PhraseReadOutcome<PhraseContextBranchesResponse>> HandleAsync(
        GetPhraseContextBranchesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!parser.TryParseSelection(query.Resolution, query.Previous, query.Following, out var selection)
            || selection is null)
        {
            return new PhraseReadOutcome<PhraseContextBranchesResponse>.Invalid(PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseContextRequestParser.TryPageSize(query.PreviousPageSize, out var previousPageSize)
            || !PhraseContextRequestParser.TryPageSize(query.FollowingPageSize, out var followingPageSize))
        {
            return new PhraseReadOutcome<PhraseContextBranchesResponse>.Invalid(PhraseRequestInvalidKind.Paging);
        }

        var scope = codec.ComputeScope(selection);
        if (!parser.TryParseCursor(
                query.PreviousCursor,
                selection.Resolution.BuildId,
                PhraseCursorKind.PreviousBranches,
                scope,
                out var previousOffset)
            || !parser.TryParseCursor(
                query.FollowingCursor,
                selection.Resolution.BuildId,
                PhraseCursorKind.FollowingBranches,
                scope,
                out var followingOffset))
        {
            return new PhraseReadOutcome<PhraseContextBranchesResponse>.Invalid(PhraseRequestInvalidKind.Cursor);
        }

        var result = await reader.GetBranchesAsync(
            selection,
            new PhraseContextBranchPaging(
                previousOffset,
                followingOffset,
                previousPageSize,
                followingPageSize),
            cancellationToken);
        return Map(result);
    }

    private static PhraseReadOutcome<PhraseContextBranchesResponse> Map(
        PhraseSearchReadResult<PhraseContextBranchesResponse> result) => result switch
    {
        PhraseSearchReadResult<PhraseContextBranchesResponse>.Success success =>
            new PhraseReadOutcome<PhraseContextBranchesResponse>.Success(success.Value),
        PhraseSearchReadResult<PhraseContextBranchesResponse>.Unavailable =>
            new PhraseReadOutcome<PhraseContextBranchesResponse>.Unavailable(),
        PhraseSearchReadResult<PhraseContextBranchesResponse>.BuildChanged =>
            new PhraseReadOutcome<PhraseContextBranchesResponse>.BuildChanged(),
        PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference =>
            new PhraseReadOutcome<PhraseContextBranchesResponse>.Invalid(PhraseRequestInvalidKind.Reference),
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseContextBranchesResponse>)} variant."),
    };
}
