using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader(
    QuranDashboardDbContext db,
    IPhraseSearchReferenceCodec codec,
    PhraseSearchReadCache cache) : IPhraseContextReader
{
    private readonly QuranDashboardDbContext db = db;
    private readonly IPhraseSearchReferenceCodec codec = codec;
    private readonly PhraseSearchReadCache cache = cache;

    private async Task<PopulationLoadResult> LoadPopulationAsync(
        PhraseSearchReadSnapshot snapshot,
        PhraseResolutionReference resolution,
        CancellationToken cancellationToken)
    {
        var exactTokenIds = resolution.ExactTokenIds.ToArray();
        var variant = await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(candidate => candidate.BuildId == snapshot.ActiveBuildId
                && candidate.Mode == resolution.Mode
                && candidate.WordCount == exactTokenIds.Length
                && candidate.ExactTokenIds.SequenceEqual(exactTokenIds))
            .Select(candidate => new QueryVariantRow(candidate.Id, candidate.WordCount))
            .SingleOrDefaultAsync(cancellationToken);
        if (variant is null)
        {
            return new PopulationLoadResult(false, []);
        }

        var occurrenceRows = await (
            from occurrence in db.QuranPhraseOccurrences.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking()
                on occurrence.AyahId equals ayah.Id
            join surah in db.QuranSurahs.AsNoTracking()
                on ayah.SurahNumber equals surah.SurahNumber
            where occurrence.BuildId == snapshot.ActiveBuildId
                && occurrence.VariantId == variant.VariantId
            orderby ayah.SurahNumber, ayah.AyahNumber, occurrence.StartWordNumber, occurrence.Id
            select new ContextOccurrenceRow(
                occurrence.Id,
                ayah.Id,
                ayah.VerseKey,
                ayah.SurahNumber,
                surah.NameArabic,
                ayah.AyahNumber,
                ayah.PageFrom,
                ayah.PageTo,
                occurrence.StartWordNumber,
                occurrence.EndWordNumber))
            .ToListAsync(cancellationToken);
        var ayahIds = occurrenceRows.Select(row => row.AyahId).Distinct().ToList();
        var wordRows = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new ContextWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani,
                resolution.Mode == PhraseTextMode.Simple
                    ? word.UniqueSimpleWordId
                    : word.UniqueTashkeelWordId))
            .ToListAsync(cancellationToken);
        if (wordRows.Any(word => word.ExactTokenId is null))
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        var wordsByAyah = wordRows
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ContextWord>)group
                    .Select(word => new ContextWord(
                        word.QuranWordId,
                        word.WordNumber,
                        word.PageNumber,
                        word.TextUthmani,
                        word.ExactTokenId!.Value))
                    .ToList());
        var occurrences = occurrenceRows
            .Select(row => new ContextOccurrence(
                row,
                wordsByAyah.GetValueOrDefault(row.AyahId)
                    ?? throw new InvalidDataException("PhraseSearch context occurrence has no source words.")))
            .ToList();
        return new PopulationLoadResult(true, occurrences);
    }

    private static IReadOnlyList<ContextOccurrence> ApplySelection(
        IReadOnlyList<ContextOccurrence> population,
        PhraseContextSelection selection) => population
        .Where(occurrence => MatchesPath(occurrence, selection.Previous)
            && MatchesPath(occurrence, selection.Following))
        .ToList();

    private static bool MatchesPath(ContextOccurrence occurrence, PhrasePathReference? path)
    {
        if (path is null)
        {
            return true;
        }

        for (var index = 0; index < path.SelectedExactTokenIds.Count; index++)
        {
            var wordIndex = path.Side == PhraseContextSide.Previous
                ? occurrence.Row.StartWordNumber - 2 - index
                : occurrence.Row.EndWordNumber + index;
            if (wordIndex < 0
                || wordIndex >= occurrence.Words.Count
                || occurrence.Words[wordIndex].ExactTokenId != path.SelectedExactTokenIds[index])
            {
                return false;
            }
        }

        if (!path.EndsAtBoundary)
        {
            return true;
        }

        return path.Side == PhraseContextSide.Previous
            ? occurrence.Row.StartWordNumber - 1 == path.SelectedExactTokenIds.Count
            : occurrence.Words.Count - occurrence.Row.EndWordNumber == path.SelectedExactTokenIds.Count;
    }

    private static bool IsAtBoundary(
        ContextOccurrence occurrence,
        PhraseContextSide side,
        int selectedCount) => side == PhraseContextSide.Previous
        ? occurrence.Row.StartWordNumber - 1 == selectedCount
        : occurrence.Words.Count - occurrence.Row.EndWordNumber == selectedCount;

    private static ContextWord GetPathWord(
        ContextOccurrence occurrence,
        PhraseContextSide side,
        int selectedCount)
    {
        var wordIndex = side == PhraseContextSide.Previous
            ? occurrence.Row.StartWordNumber - 2 - selectedCount
            : occurrence.Row.EndWordNumber + selectedCount;
        return occurrence.Words[wordIndex];
    }

    private PhraseResolvedQueryDto CreateResolvedQuery(
        PhraseResolutionReference resolution,
        ContextOccurrence occurrence)
    {
        var tokens = Enumerable.Range(occurrence.Row.StartWordNumber - 1, resolution.ExactTokenIds.Count)
            .Select(index => occurrence.Words[index])
            .Select(ToExactToken)
            .ToList();
        if (!tokens.Select(token => token.ExactTokenId).SequenceEqual(resolution.ExactTokenIds))
        {
            throw new InvalidDataException("PhraseSearch context occurrence does not match its resolved exact identity.");
        }

        return new PhraseResolvedQueryDto(
            codec.EncodeResolution(resolution),
            PhraseTextModeContract.CanonicalKey(resolution.Mode),
            tokens);
    }

    private PhraseSelectedPathDto CreateSelectedPath(
        PhraseResolutionReference resolution,
        PhrasePathReference? path,
        PhraseContextSide side,
        ContextOccurrence occurrence)
    {
        if (path is null)
        {
            return new PhraseSelectedPathDto(null, false, [], []);
        }

        var tokens = Enumerable.Range(0, path.SelectedExactTokenIds.Count)
            .Select(index => GetSelectedPathWord(occurrence, side, index))
            .Select(ToExactToken)
            .ToList();
        if (!tokens.Select(token => token.ExactTokenId).SequenceEqual(path.SelectedExactTokenIds))
        {
            throw new InvalidDataException("PhraseSearch selected path does not match the filtered occurrence population.");
        }

        return new PhraseSelectedPathDto(
            codec.EncodePath(path),
            path.EndsAtBoundary,
            tokens,
            CreateSelectedPathSteps(path, side, tokens));
    }

    private IReadOnlyList<PhraseSelectedPathStepDto> CreateSelectedPathSteps(
        PhrasePathReference path,
        PhraseContextSide side,
        IReadOnlyList<PhraseExactTokenDto> tokens)
    {
        var steps = tokens
            .Select((token, index) => new PhraseSelectedPathStepDto(
                codec.EncodePath(new PhrasePathReference(
                    path.BuildId,
                    path.Mode,
                    path.Side,
                    path.QueryExactTokenIds,
                    path.SelectedExactTokenIds.Take(index + 1).ToArray(),
                    false)),
                token.TextUthmani,
                null))
            .ToList();
        if (path.EndsAtBoundary)
        {
            steps.Add(new PhraseSelectedPathStepDto(
                codec.EncodePath(path),
                side == PhraseContextSide.Previous ? "بداية الآية" : "نهاية الآية",
                side == PhraseContextSide.Previous
                    ? PhraseContextBoundaryKinds.AyahStart
                    : PhraseContextBoundaryKinds.AyahEnd));
        }
        return steps;
    }

    private static ContextWord GetSelectedPathWord(
        ContextOccurrence occurrence,
        PhraseContextSide side,
        int selectedIndex)
    {
        var wordIndex = side == PhraseContextSide.Previous
            ? occurrence.Row.StartWordNumber - 2 - selectedIndex
            : occurrence.Row.EndWordNumber + selectedIndex;
        return occurrence.Words[wordIndex];
    }

    private static PhraseExactTokenDto ToExactToken(ContextWord word) =>
        new(word.ExactTokenId, word.TextUthmani);

    private static IReadOnlyList<int> FullPathIds(
        ContextOccurrence occurrence,
        PhraseContextSide side)
    {
        if (side == PhraseContextSide.Previous)
        {
            return occurrence.Words
                .Take(occurrence.Row.StartWordNumber - 1)
                .Reverse()
                .Select(word => word.ExactTokenId)
                .ToArray();
        }

        return occurrence.Words
            .Skip(occurrence.Row.EndWordNumber)
            .Select(word => word.ExactTokenId)
            .ToArray();
    }

    private static IReadOnlyList<PhraseExactTokenDto> FullPathTokens(
        ContextOccurrence occurrence,
        PhraseContextSide side)
    {
        if (side == PhraseContextSide.Previous)
        {
            return occurrence.Words
                .Take(occurrence.Row.StartWordNumber - 1)
                .Reverse()
                .Select(ToExactToken)
                .ToList();
        }

        return occurrence.Words
            .Skip(occurrence.Row.EndWordNumber)
            .Select(ToExactToken)
            .ToList();
    }

    private string? CreateNextCursor(
        Guid buildId,
        PhraseCursorKind kind,
        int offset,
        int pageSize,
        int totalCount,
        ulong scope)
    {
        var nextOffset = (long)offset + pageSize;
        return nextOffset < totalCount
            ? codec.EncodeCursor(new PhraseCursorReference(buildId, kind, checked((int)nextOffset), scope))
            : null;
    }

    private sealed record PopulationLoadResult(bool QueryExists, IReadOnlyList<ContextOccurrence> Occurrences);
    private sealed record QueryVariantRow(long VariantId, short WordCount);
    private sealed record ContextOccurrence(ContextOccurrenceRow Row, IReadOnlyList<ContextWord> Words);
    private sealed record ContextOccurrenceRow(
        long OccurrenceId,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        string SurahNameArabic,
        short AyahNumber,
        short PageFrom,
        short PageTo,
        short StartWordNumber,
        short EndWordNumber);
    private sealed record ContextWord(
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        int ExactTokenId);
    private sealed record ContextWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        int? ExactTokenId);
}
