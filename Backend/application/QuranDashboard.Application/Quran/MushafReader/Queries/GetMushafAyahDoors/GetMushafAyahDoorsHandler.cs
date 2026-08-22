using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafAyahDoors;

public sealed partial class GetMushafAyahDoorsHandler(IMushafAyahDoorsReader reader)
{
    public async Task<GetMushafAyahDoorsOutcome> HandleAsync(
        GetMushafAyahDoorsQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!VerseKeyPattern().IsMatch(query.VerseKey))
        {
            return new GetMushafAyahDoorsOutcome.InvalidVerseKey();
        }

        var response = await reader.GetDoorsAsync(query.VerseKey, ct);
        return response is null
            ? new GetMushafAyahDoorsOutcome.NotFound()
            : new GetMushafAyahDoorsOutcome.Success(response);
    }

    [GeneratedRegex(@"^\d+:\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex VerseKeyPattern();
}
