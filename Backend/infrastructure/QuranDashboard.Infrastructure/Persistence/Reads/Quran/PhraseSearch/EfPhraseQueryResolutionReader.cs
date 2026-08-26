using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed class EfPhraseQueryResolutionReader(
    QuranDashboardDbContext db,
    IPhraseSearchReferenceCodec codec) : IPhraseQueryResolutionReader
{
    public async Task<PhraseSearchReadResult<PhraseQueryResolutionResponse>> ResolveAsync(
        PhraseTextMode mode,
        IReadOnlyList<string> normalizedSegments,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseQueryResolutionResponse>.Unavailable();
        }

        var candidates = await ResolveCandidatesAsync(
            snapshot.ActiveBuildId,
            mode,
            normalizedSegments,
            cancellationToken);
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
        return new PhraseSearchReadResult<PhraseQueryResolutionResponse>.Success(response);
    }

    private async Task<IReadOnlyList<ResolutionVariant>> ResolveCandidatesAsync(
        Guid buildId,
        PhraseTextMode mode,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        var maximumSearchTextLength = await db.QuranPhraseSearchTokens
            .AsNoTracking()
            .Where(token => token.BuildId == buildId && token.Mode == mode)
            .MaxAsync(token => token.SearchText.Length, cancellationToken);
        var possibleTexts = CreatePossibleTexts(segments, maximumSearchTextLength);
        var tokenRows = await db.QuranPhraseSearchTokens
            .AsNoTracking()
            .Where(token => token.BuildId == buildId
                && token.Mode == mode
                && possibleTexts.Contains(token.SearchText))
            .Select(token => new SearchTokenRow(token.Id, token.SearchText))
            .ToListAsync(cancellationToken);
        var tokensByText = tokenRows.ToDictionary(token => token.SearchText, StringComparer.Ordinal);
        var tokenizations = CreateTokenizations(segments, tokensByText, maximumSearchTextLength);
        var results = new Dictionary<string, ResolutionVariant>(StringComparer.Ordinal);

        foreach (var costGroup in tokenizations.GroupBy(tokenization => tokenization.JoinedBoundaryCount))
        {
            foreach (var tokenization in costGroup)
            {
                var searchTokenIds = tokenization.SearchTokenIds;
                var rows = await db.QuranPhraseVariants
                    .AsNoTracking()
                    .Where(variant => variant.BuildId == buildId
                        && variant.Mode == mode
                        && variant.WordCount == searchTokenIds.Length
                        && variant.SearchTokenIds.SequenceEqual(searchTokenIds))
                    .Select(variant => new ResolutionVariant(
                        variant.Id,
                        variant.WordCount,
                        variant.ExactTokenIds,
                        variant.DisplayText,
                        variant.FirstQuranWordId))
                    .ToListAsync(cancellationToken);

                foreach (var row in rows)
                {
                    results.TryAdd(TokenKey(row.ExactTokenIds), row);
                }
            }

            if (results.Count > 0)
            {
                break;
            }
        }

        return results.Values
            .OrderBy(candidate => candidate.WordCount)
            .ThenBy(candidate => candidate.ExactTokenIds, IntSequenceComparer.Instance)
            .Take(PhraseSearchQueryLimits.MaximumResolutionCandidates)
            .ToList();
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

    private static IReadOnlySet<string> CreatePossibleTexts(
        IReadOnlyList<string> segments,
        int maximumLength)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        for (var start = 0; start < segments.Count; start++)
        {
            var builder = new StringBuilder();
            for (var end = start; end < segments.Count; end++)
            {
                builder.Append(segments[end]);
                if (builder.Length > maximumLength)
                {
                    break;
                }

                values.Add(builder.ToString());
            }
        }

        return values;
    }

    private static IReadOnlyList<Tokenization> CreateTokenizations(
        IReadOnlyList<string> segments,
        IReadOnlyDictionary<string, SearchTokenRow> tokensByText,
        int maximumLength)
    {
        var results = new List<Tokenization>();
        var current = new List<int>();

        void Visit(int start, int joinedBoundaryCount)
        {
            if (start == segments.Count)
            {
                results.Add(new Tokenization(joinedBoundaryCount, current.ToArray()));
                return;
            }

            var builder = new StringBuilder();
            for (var end = start; end < segments.Count; end++)
            {
                builder.Append(segments[end]);
                if (builder.Length > maximumLength)
                {
                    break;
                }

                if (!tokensByText.TryGetValue(builder.ToString(), out var token))
                {
                    continue;
                }

                current.Add(checked((int)token.Id));
                Visit(end + 1, joinedBoundaryCount + end - start);
                current.RemoveAt(current.Count - 1);
            }
        }

        Visit(0, 0);
        return results
            .OrderBy(result => result.JoinedBoundaryCount)
            .ThenBy(result => result.SearchTokenIds, IntSequenceComparer.Instance)
            .ToList();
    }

    private static string TokenKey(IReadOnlyList<int> values) => string.Join(',', values);

    private sealed record SearchTokenRow(long Id, string SearchText);
    private sealed record Tokenization(int JoinedBoundaryCount, int[] SearchTokenIds);
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

    private sealed class IntSequenceComparer : IComparer<IReadOnlyList<int>>, IComparer<int[]>
    {
        internal static IntSequenceComparer Instance { get; } = new();

        public int Compare(IReadOnlyList<int>? left, IReadOnlyList<int>? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            for (var index = 0; index < Math.Min(left.Count, right.Count); index++)
            {
                var comparison = left[index].CompareTo(right[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Count.CompareTo(right.Count);
        }

        public int Compare(int[]? left, int[]? right) => Compare(
            (IReadOnlyList<int>?)left,
            right);
    }
}
