using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

public sealed partial class GetSimilarAyahsHandler(IAyahSimilaritiesReader ayahSimilaritiesReader)
{
    public async Task<GetSimilarAyahsOutcome> HandleAsync(GetSimilarAyahsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!VerseKeyPattern().IsMatch(query.VerseKey))
        {
            return new GetSimilarAyahsOutcome.InvalidVerseKey();
        }

        var response = await ayahSimilaritiesReader.GetSimilarAyahsAsync(query.VerseKey, ct);

        return response is null
            ? new GetSimilarAyahsOutcome.NotFound()
            : new GetSimilarAyahsOutcome.Success(response);
    }

    [GeneratedRegex(@"^\d+:\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex VerseKeyPattern();
}
