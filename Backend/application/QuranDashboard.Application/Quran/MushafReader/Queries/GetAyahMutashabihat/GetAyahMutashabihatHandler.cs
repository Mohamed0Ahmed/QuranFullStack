using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

public sealed partial class GetAyahMutashabihatHandler(IAyahMutashabihatReader ayahMutashabihatReader)
{
    public async Task<GetAyahMutashabihatOutcome> HandleAsync(GetAyahMutashabihatQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!VerseKeyPattern().IsMatch(query.VerseKey))
        {
            return new GetAyahMutashabihatOutcome.InvalidVerseKey();
        }

        var response = await ayahMutashabihatReader.GetAyahMutashabihatAsync(query.VerseKey, ct);

        return response is null
            ? new GetAyahMutashabihatOutcome.NotFound()
            : new GetAyahMutashabihatOutcome.Success(response);
    }

    [GeneratedRegex(@"^\d+:\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex VerseKeyPattern();
}
