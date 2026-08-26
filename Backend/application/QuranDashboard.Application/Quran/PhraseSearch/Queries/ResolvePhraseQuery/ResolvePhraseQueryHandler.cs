using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseQuery;

public sealed class ResolvePhraseQueryHandler(IPhraseQueryResolutionReader reader)
{
    public async Task<PhraseReadOutcome<PhraseQueryResolutionResponse>> HandleAsync(
        ResolvePhraseQueryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!PhraseTextModeContract.TryParse(query.Mode, out var mode))
        {
            return new PhraseReadOutcome<PhraseQueryResolutionResponse>.Invalid(PhraseRequestInvalidKind.Mode);
        }

        var parsed = PhraseQueryInputParser.Parse(query.Q64, mode);
        if (parsed is PhraseQueryParseResult.Failure failure)
        {
            return new PhraseReadOutcome<PhraseQueryResolutionResponse>.Invalid(failure.Kind);
        }

        var segments = ((PhraseQueryParseResult.Success)parsed).Segments;
        var result = await reader.ResolveAsync(mode, segments, cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseQueryResolutionResponse>.Success success =>
                new PhraseReadOutcome<PhraseQueryResolutionResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseQueryResolutionResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseQueryResolutionResponse>.Unavailable(),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseQueryResolutionResponse>)} variant."),
        };
    }
}
