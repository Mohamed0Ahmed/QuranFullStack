using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextResults;

public sealed class GetPhraseContextResultsHandler(
    IPhraseContextReader reader,
    PhraseContextRequestParser parser)
{
    public async Task<PhraseReadOutcome<PhraseContextResultsResponse>> HandleAsync(
        GetPhraseContextResultsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!parser.TryParseSelection(query.Resolution, query.Previous, query.Following, out var selection)
            || selection is null)
        {
            return new PhraseReadOutcome<PhraseContextResultsResponse>.Invalid(PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseContextRequestParser.TryResultPageSize(query.PageSize, out var pageSize))
        {
            return new PhraseReadOutcome<PhraseContextResultsResponse>.Invalid(PhraseRequestInvalidKind.Paging);
        }

        var result = await reader.GetResultsAsync(selection, pageSize, cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseContextResultsResponse>.Success success =>
                new PhraseReadOutcome<PhraseContextResultsResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseContextResultsResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseContextResultsResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseContextResultsResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseContextResultsResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseContextResultsResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseContextResultsResponse>.Invalid(PhraseRequestInvalidKind.Reference),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseContextResultsResponse>)} variant."),
        };
    }
}
