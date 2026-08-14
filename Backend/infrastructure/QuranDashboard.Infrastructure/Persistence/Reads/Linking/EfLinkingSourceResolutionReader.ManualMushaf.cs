using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<LinkingSourceReferenceSet> ResolveManualMushafAsync(
        LinkingSourceDescriptor.ManualMushafAyahs source,
        CancellationToken cancellationToken)
    {
        GuardResolvedAyahCount(source.VerseKeys.Count);

        var requestedVerseKeys = source.VerseKeys.Select(verseKey => verseKey.Value).ToList();

        var ayahs = await LinkingAyahHydration.LoadByVerseKeysAsync(
            _dbContext,
            requestedVerseKeys,
            cancellationToken);

        var ayahIds = ayahs.Select(ayah => ayah.AyahId).ToList();

        var proofWords = await _dbContext.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .Select(word => new ManualProofWordRow(
                word.AyahId,
                word.WordNumber,
                word.Location,
                word.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var proofWordsByAyah = proofWords
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LinkingManualAyahWord>)
                [
                    .. group.Select(word => new LinkingManualAyahWord(
                        word.WordNumber,
                        word.Location,
                        word.IsAyahMarker))
                ]);

        var ayahsByVerseKey = ayahs.ToDictionary(
            ayah => ayah.VerseKey,
            StringComparer.Ordinal);

        foreach (var requestedVerseKey in requestedVerseKeys)
        {
            var ayah = ayahsByVerseKey.GetValueOrDefault(requestedVerseKey);

            var proof = ayah is null
                ? null
                : new LinkingManualAyah(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    ayah.WordsCountReal,
                    proofWordsByAyah.GetValueOrDefault(ayah.AyahId, []));

            var violation = LinkingManualAyahCompleteness.Verify(requestedVerseKey, proof);
            if (violation is not null)
            {
                throw new LinkingInvalidDescriptorException(violation);
            }
        }

        return new LinkingSourceReferenceSet(ayahIds, [], true);
    }

    private sealed record ManualProofWordRow(
        int AyahId,
        int WordNumber,
        string Location,
        bool IsAyahMarker);
}
