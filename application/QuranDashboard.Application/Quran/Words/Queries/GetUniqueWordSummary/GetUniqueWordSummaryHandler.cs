using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

/// <summary>
/// Validates the selected unique-word summary request and delegates to
/// <see cref="IUniqueWordsReader"/>. Mirrors the established handler shape:
/// parse kind → validate ID bounds → read → map to a discriminated outcome.
/// </summary>
public sealed class GetUniqueWordSummaryHandler(IUniqueWordsReader reader)
{
    public async Task<GetUniqueWordSummaryOutcome> HandleAsync(
        GetUniqueWordSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            return new GetUniqueWordSummaryOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            return new GetUniqueWordSummaryOutcome.InvalidId();
        }

        var summary = await reader.GetUniqueWordSummaryAsync(kind, query.Id, cancellationToken);
        if (summary is null)
        {
            return new GetUniqueWordSummaryOutcome.NotFound();
        }

        return new GetUniqueWordSummaryOutcome.Success(summary);
    }
}
