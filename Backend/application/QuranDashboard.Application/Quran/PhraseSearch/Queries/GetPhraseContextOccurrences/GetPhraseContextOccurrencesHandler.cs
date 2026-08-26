using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextOccurrences;

public sealed class GetPhraseContextOccurrencesHandler(
    IPhraseContextReader reader,
    IPhraseSearchReferenceCodec codec,
    PhraseContextRequestParser parser)
{
    public async Task<PhraseReadOutcome<PhraseContextOccurrencesResponse>> HandleAsync(
        GetPhraseContextOccurrencesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!codec.TryDecodeFullContext(query.Context, out var context)
            || context is null)
        {
            return new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Invalid(PhraseRequestInvalidKind.Reference);
        }

        if (!PhraseContextRequestParser.TryPageSize(query.PageSize, out var pageSize))
        {
            return new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Invalid(PhraseRequestInvalidKind.Paging);
        }

        var scope = codec.ComputeScope(context);
        if (!parser.TryParseCursor(
            query.Cursor,
            context.BuildId,
            PhraseCursorKind.ContextOccurrences,
            scope,
            out var offset))
        {
            return new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Invalid(PhraseRequestInvalidKind.Cursor);
        }

        var result = await reader.GetOccurrencesAsync(
            context,
            new PhraseCursorPage(offset, pageSize),
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseContextOccurrencesResponse>.Success success =>
                new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseContextOccurrencesResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Unavailable(),
            PhraseSearchReadResult<PhraseContextOccurrencesResponse>.BuildChanged =>
                new PhraseReadOutcome<PhraseContextOccurrencesResponse>.BuildChanged(),
            PhraseSearchReadResult<PhraseContextOccurrencesResponse>.InvalidReference =>
                new PhraseReadOutcome<PhraseContextOccurrencesResponse>.Invalid(PhraseRequestInvalidKind.Reference),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseContextOccurrencesResponse>)} variant."),
        };
    }
}
