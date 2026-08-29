using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseQueryResolutionReader(
    QuranDashboardDbContext db,
    IPhraseSearchReferenceCodec codec,
    PhraseSearchReadCache cache) : IPhraseQueryResolutionReader
{
    private readonly QuranDashboardDbContext db = db;
    private readonly IPhraseSearchReferenceCodec codec = codec;
    private readonly PhraseSearchReadCache cache = cache;

    public async Task<PhraseQueryResolutionReadResult> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseQueryResolutionReadResult.Unavailable();
        }

        var cacheKey = PhraseSearchCacheKeys.Resolution(
            snapshot.ActiveBuildId,
            mode,
            normalizedSegments);
        if (cache.TryGet(cacheKey, out PhraseQueryResolutionResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseQueryResolutionReadResult.Success(cached);
        }

        var candidateSearch = await ResolveCandidatesAsync(
            snapshot.ActiveBuildId,
            mode,
            normalizedSegments,
            cancellationToken);
        if (candidateSearch is ResolutionSearchResult.TooComplex)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseQueryResolutionReadResult.TooComplex();
        }

        var candidates = ((ResolutionSearchResult.Found)candidateSearch).Candidates;
        var displayTokensByVariant = await LoadDisplayTokensAsync(
            candidates,
            mode,
            cancellationToken);
        var candidateDtos = new List<PhraseResolutionCandidateDto>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var tokens = displayTokensByVariant[candidate.VariantId];

            var resolutionRef = codec.EncodeResolution(new PhraseResolutionReference(
                snapshot.ActiveBuildId,
                mode,
                candidate.ExactTokenIds));
            candidateDtos.Add(new PhraseResolutionCandidateDto(
                resolutionRef,
                candidate.WordCount,
                candidate.DisplayText,
                tokens));
        }

        var status = candidateDtos.Count switch
        {
            0 => PhraseResolutionStatuses.Unresolved,
            1 => PhraseResolutionStatuses.Resolved,
            _ => PhraseResolutionStatuses.Ambiguous,
        };
        var response = new PhraseQueryResolutionResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(mode),
            status,
            candidateDtos);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response);
        return new PhraseQueryResolutionReadResult.Success(response);
    }

    private async Task<IReadOnlyDictionary<long, IReadOnlyList<PhraseExactTokenDto>>> LoadDisplayTokensAsync(
        IReadOnlyList<ResolutionVariant> candidates,
        PhraseTextMode mode,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return new Dictionary<long, IReadOnlyList<PhraseExactTokenDto>>();
        }

        var firstWordIds = candidates
            .Select(candidate => candidate.FirstQuranWordId)
            .Distinct()
            .ToList();
        var firstWords = await db.QuranWords
            .AsNoTracking()
            .Where(word => firstWordIds.Contains(word.Id) && !word.IsAyahMarker)
            .Select(word => new ResolutionFirstWordRow(word.Id, word.AyahId, word.WordNumber))
            .ToListAsync(cancellationToken);
        if (firstWords.Count != firstWordIds.Count)
        {
            throw new InvalidDataException("PhraseSearch resolution candidate has no readable first source token.");
        }

        var ayahIds = firstWords.Select(word => word.AyahId).Distinct().ToList();
        var words = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.AyahId)
            .ThenBy(word => word.WordNumber)
            .Select(word => new ResolutionDisplayWordRow(
                word.AyahId,
                word.WordNumber,
                mode == PhraseTextMode.Simple
                    ? word.UniqueSimpleWordId
                    : word.UniqueTashkeelWordId,
                word.TextUthmani))
            .ToListAsync(cancellationToken);
        var firstWordsById = firstWords.ToDictionary(word => word.QuranWordId);
        var wordsByAyah = words
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var result = new Dictionary<long, IReadOnlyList<PhraseExactTokenDto>>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var firstWord = firstWordsById[candidate.FirstQuranWordId];
            var endWordNumber = checked((short)(firstWord.WordNumber + candidate.WordCount - 1));
            var tokens = wordsByAyah[firstWord.AyahId]
                .Where(word => word.WordNumber >= firstWord.WordNumber
                    && word.WordNumber <= endWordNumber)
                .Select(word => new PhraseExactTokenDto(
                    word.ExactTokenId
                        ?? throw new InvalidDataException("PhraseSearch source token has no exact identity."),
                    word.TextUthmani))
                .ToList();
            if (tokens.Count != candidate.WordCount
                || !tokens.Select(token => token.ExactTokenId).SequenceEqual(candidate.ExactTokenIds))
            {
                throw new InvalidDataException("PhraseSearch resolution candidate display does not attest its exact identity.");
            }

            result.Add(candidate.VariantId, tokens);
        }

        return result;
    }

    private sealed record ResolutionVariant(
        long VariantId,
        short WordCount,
        int[] ExactTokenIds,
        string DisplayText,
        int FirstQuranWordId);
    private sealed record ResolutionFirstWordRow(
        int QuranWordId,
        int AyahId,
        short WordNumber);
    private sealed record ResolutionDisplayWordRow(
        int AyahId,
        short WordNumber,
        int? ExactTokenId,
        string TextUthmani);
}
