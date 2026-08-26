using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextGroups;

public sealed class GetPhraseContextGroupsHandler(
    IPhraseContextReader reader,
    IPhraseSearchReferenceCodec codec,
    PhraseContextRequestParser parser)
{
    public async Task<PhraseReadOutcome<PhraseContextGroupsResponse>> HandleAsync(
        GetPhraseContextGroupsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!parser.TryParseSelection(query.Resolution, query.Previous, query.Following, out var selection)
            || selection is null)
        {
            return new PhraseReadOutcome<PhraseContextGroupsResponse>.Invalid(PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseContextRequestParser.TryPageSize(query.PageSize, out var pageSize))
        {
            return new PhraseReadOutcome<PhraseContextGroupsResponse>.Invalid(PhraseRequestInvalidKind.Paging);
        }

        var scope = codec.ComputeScope(selection);
        if (!parser.TryParseCursor(
            query.Cursor,
            selection.Resolution.BuildId,
            PhraseCursorKind.ContextGroups,
            scope,
            out var offset))
        {
            return new PhraseReadOutcome<PhraseContextGroupsResponse>.Invalid(PhraseRequestInvalidKind.Cursor);
        }

        var result = await reader.GetGroupsAsync(
            selection,
            new PhraseCursorPage(offset, pageSize),
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseContextGroupsResponse>.Success success =>
                new PhraseReadOutcome<PhraseContextGroupsResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseContextGroupsResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseContextGroupsResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseContextGroupsResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseContextGroupsResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseContextGroupsResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseContextGroupsResponse>.Invalid(PhraseRequestInvalidKind.Reference),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseContextGroupsResponse>)} variant."),
        };
    }
}
