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

    private PhraseResolvedQueryDto CreateResolvedQuery(
        PhraseResolutionReference resolution,
        IReadOnlyDictionary<int, string> tokenTexts) => new(
        codec.EncodeResolution(resolution),
        PhraseTextModeContract.CanonicalKey(resolution.Mode),
        CreateExactTokens(resolution.ExactTokenIds, tokenTexts));

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

    private PhraseSelectedPathDto CreateSelectedPath(
        PhrasePathReference? path,
        PhraseContextSide side,
        IReadOnlyDictionary<int, string> tokenTexts)
    {
        if (path is null)
        {
            return new PhraseSelectedPathDto(null, false, [], []);
        }

        var tokens = CreateExactTokens(path.SelectedExactTokenIds, tokenTexts);
        return new PhraseSelectedPathDto(
            codec.EncodePath(path),
            path.EndsAtBoundary,
            tokens,
            CreateSelectedPathSteps(path, side, tokens));
    }

    private static IReadOnlyList<PhraseExactTokenDto> CreateExactTokens(
        IEnumerable<int> exactTokenIds,
        IReadOnlyDictionary<int, string> tokenTexts) => exactTokenIds
        .Select(exactTokenId => tokenTexts.TryGetValue(exactTokenId, out var textUthmani)
            ? new PhraseExactTokenDto(exactTokenId, textUthmani)
            : throw new InvalidDataException("PhraseSearch context token has no canonical display text."))
        .ToList();

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

}
