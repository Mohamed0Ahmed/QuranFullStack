using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;

/// <summary>
/// Validates the missing-surahs request and delegates to
/// <see cref="IUniqueWordsReader"/>.
/// </summary>
public sealed class GetUniqueWordMissingSurahsHandler(IUniqueWordsReader reader)
{
    public async Task<GetUniqueWordMissingSurahsOutcome> HandleAsync(
        GetUniqueWordMissingSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            return new GetUniqueWordMissingSurahsOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            return new GetUniqueWordMissingSurahsOutcome.InvalidId();
        }

        var response = await reader.GetMissingSurahsAsync(kind, query.Id, cancellationToken);
        if (response is null)
        {
            return new GetUniqueWordMissingSurahsOutcome.NotFound();
        }

        return new GetUniqueWordMissingSurahsOutcome.Success(response);
    }
}
