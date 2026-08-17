using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveUniqueWordAsync(
        LinkingSourceDescriptor.UniqueWord source,
        CancellationToken cancellationToken)
    {
        var tashkeel = source.Mode == LinkingUniqueWordMode.Tashkeel;
        var typeCodes = source.TypeCodes.ToArray();

        var wordExists = tashkeel
            ? await _dbContext.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .AnyAsync(word => word.Id == source.WordId, cancellationToken)
            : await _dbContext.QuranWordsUniqueSimple
                .AsNoTracking()
                .AnyAsync(word => word.Id == source.WordId, cancellationToken);
        if (!wordExists)
        {
            throw NotFound(tashkeel ? "tashkeelWordId" : "simpleWordId", source.WordId);
        }

        var readableMatches = tashkeel
            ? _dbContext.QuranWords.AsNoTracking()
                .Where(word =>
                    !word.IsAyahMarker
                    && word.UniqueTashkeelWordId == source.WordId
                    && (typeCodes.Length == 0 || _dbContext.WordMorphologies.Any(
                        morphology => morphology.QuranWordId == word.Id
                            && typeCodes.Contains(morphology.HeadPos))))
            : _dbContext.QuranWords.AsNoTracking()
                .Where(word =>
                    !word.IsAyahMarker
                    && word.UniqueSimpleWordId == source.WordId
                    && (typeCodes.Length == 0 || _dbContext.WordMorphologies.Any(
                        morphology => morphology.QuranWordId == word.Id
                            && typeCodes.Contains(morphology.HeadPos))));

        return await readableMatches
            .Select(word => new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
