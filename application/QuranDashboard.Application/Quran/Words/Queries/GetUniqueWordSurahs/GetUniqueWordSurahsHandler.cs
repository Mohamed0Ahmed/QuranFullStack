using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;

public sealed class GetUniqueWordSurahsHandler(IUniqueWordsReader reader)
{
    public async Task<GetUniqueWordSurahsOutcome> HandleAsync(
        GetUniqueWordSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            return new GetUniqueWordSurahsOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            return new GetUniqueWordSurahsOutcome.InvalidId();
        }

        var response = await reader.GetMentionedSurahsAsync(kind, query.Id, cancellationToken);
        if (response is null)
        {
            return new GetUniqueWordSurahsOutcome.NotFound();
        }

        return new GetUniqueWordSurahsOutcome.Success(response);
    }
}
