using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

public sealed partial class GetAyahStudyHandler(
    IAyahStudyReader ayahStudyReader,
    MushafReaderOptions options)
{
    public async Task<GetAyahStudyOutcome> HandleAsync(GetAyahStudyQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!VerseKeyPattern().IsMatch(query.VerseKey))
        {
            return new GetAyahStudyOutcome.InvalidVerseKey();
        }

        var resolvedTafsir = ResolveSource(query.TafsirSource, options.DefaultTafsirSourceKey);
        var resolvedTranslation = ResolveSource(query.TranslationSource, options.DefaultTranslationSourceKey);
        var resolvedFullI3rab = ResolveSource(query.FullI3rabSource, options.DefaultFullI3rabSourceKey);

        var response = await ayahStudyReader.GetAyahStudyAsync(
            query.VerseKey,
            resolvedTafsir,
            resolvedTranslation,
            resolvedFullI3rab,
            ct);

        return response is null
            ? new GetAyahStudyOutcome.NotFound()
            : new GetAyahStudyOutcome.Success(response);
    }

    private static string? ResolveSource(string? explicitSource, string? defaultSource) =>
        !string.IsNullOrWhiteSpace(explicitSource)
            ? explicitSource
            : !string.IsNullOrWhiteSpace(defaultSource)
                ? defaultSource
                : null;

    [GeneratedRegex(@"^\d+:\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex VerseKeyPattern();
}
