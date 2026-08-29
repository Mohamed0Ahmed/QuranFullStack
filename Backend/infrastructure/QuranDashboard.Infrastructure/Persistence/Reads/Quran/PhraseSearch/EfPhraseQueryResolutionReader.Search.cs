using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseQueryResolutionReader
{
    private const string ResolutionVariantsSql = """
        WITH tokenizations AS (
            SELECT ARRAY(
                SELECT element.value::integer
                FROM jsonb_array_elements_text(candidate.value)
                    WITH ORDINALITY AS element(value, position)
                ORDER BY element.position
            ) AS search_token_ids
            FROM jsonb_array_elements(@tokenizations::jsonb) AS candidate(value)
        )
        SELECT
            variant.id AS "VariantId",
            variant.word_count AS "WordCount",
            variant.exact_token_ids AS "ExactTokenIds",
            variant.display_text AS "DisplayText",
            variant.first_quran_word_id AS "FirstQuranWordId"
        FROM quran_phrase_variants AS variant
        INNER JOIN tokenizations AS tokenization
            ON tokenization.search_token_ids = variant.search_token_ids
        WHERE variant.build_id = @buildId
          AND variant.mode = @mode
          AND variant.word_count = @wordCount
        ORDER BY variant.exact_token_ids
        LIMIT @candidateLimit
        """;

    private async Task<ResolutionSearchResult> ResolveCandidatesAsync(
        Guid buildId,
        PhraseTextMode mode,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        var maximumSearchTextLength = await db.QuranPhraseSearchTokens
            .AsNoTracking()
            .Where(token => token.BuildId == buildId && token.Mode == mode)
            .MaxAsync(token => token.SearchText.Length, cancellationToken);
        var possibleTexts = CreatePossibleTexts(
            segments,
            maximumSearchTextLength,
            cancellationToken);
        var tokenRows = await db.QuranPhraseSearchTokens
            .AsNoTracking()
            .Where(token => token.BuildId == buildId
                && token.Mode == mode
                && possibleTexts.Contains(token.SearchText))
            .Select(token => new SearchTokenRow(token.Id, token.SearchText))
            .ToListAsync(cancellationToken);
        var tokensByText = tokenRows.ToDictionary(token => token.SearchText, StringComparer.Ordinal);
        var matchesByStart = CreateTokenMatches(
            segments,
            tokensByText,
            maximumSearchTextLength,
            cancellationToken);
        return await SearchCandidatesAsync(
            buildId,
            mode,
            segments.Count,
            matchesByStart,
            cancellationToken);
    }

    private async Task<ResolutionSearchResult> SearchCandidatesAsync(
        Guid buildId,
        PhraseTextMode mode,
        int segmentCount,
        IReadOnlyList<TokenMatch>[] matchesByStart,
        CancellationToken cancellationToken)
    {
        var initialState = new SearchState(0, 0, []);
        var frontier = new SortedSet<SearchState>(SearchStateOrderComparer.Instance)
        {
            initialState,
        };
        var discovered = new HashSet<SearchState>(SearchStateIdentityComparer.Instance)
        {
            initialState,
        };
        var pendingTokenizations = new List<int[]>(
            PhraseSearchQueryLimits.MaximumResolutionTokenizationBatchSize);
        var results = new Dictionary<string, ResolutionVariant>(StringComparer.Ordinal);
        var completedTokenizationCount = 0;
        int? completionCost = null;

        while (frontier.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = frontier.Min!;
            frontier.Remove(state);

            if (completionCost is int completedCost
                && state.JoinedBoundaryCount > completedCost)
            {
                await LoadResolutionBatchAsync(
                    buildId,
                    mode,
                    pendingTokenizations,
                    results,
                    cancellationToken);
                pendingTokenizations.Clear();
                if (results.Count > 0)
                {
                    return new ResolutionSearchResult.Found(OrderCandidates(results.Values));
                }

                completionCost = null;
            }

            if (state.NextSegmentIndex == segmentCount)
            {
                completionCost ??= state.JoinedBoundaryCount;
                completedTokenizationCount++;
                if (completedTokenizationCount
                    > PhraseSearchQueryLimits.MaximumResolutionCompletedTokenizations)
                {
                    return new ResolutionSearchResult.TooComplex();
                }

                pendingTokenizations.Add(state.SearchTokenIds);
                if (pendingTokenizations.Count
                    == PhraseSearchQueryLimits.MaximumResolutionTokenizationBatchSize)
                {
                    await LoadResolutionBatchAsync(
                        buildId,
                        mode,
                        pendingTokenizations,
                        results,
                        cancellationToken);
                    pendingTokenizations.Clear();
                }

                continue;
            }

            foreach (var match in matchesByStart[state.NextSegmentIndex])
            {
                cancellationToken.ThrowIfCancellationRequested();
                var nextState = new SearchState(
                    match.NextSegmentIndex,
                    state.JoinedBoundaryCount + match.JoinedBoundaryCount,
                    Append(state.SearchTokenIds, match.SearchTokenId));
                if (!discovered.Add(nextState))
                {
                    continue;
                }

                if (discovered.Count
                    > PhraseSearchQueryLimits.MaximumResolutionExplorationStates)
                {
                    return new ResolutionSearchResult.TooComplex();
                }

                frontier.Add(nextState);
            }
        }

        await LoadResolutionBatchAsync(
            buildId,
            mode,
            pendingTokenizations,
            results,
            cancellationToken);
        return new ResolutionSearchResult.Found(OrderCandidates(results.Values));
    }

    private async Task LoadResolutionBatchAsync(
        Guid buildId,
        PhraseTextMode mode,
        IReadOnlyList<int[]> tokenizations,
        IDictionary<string, ResolutionVariant> results,
        CancellationToken cancellationToken)
    {
        if (tokenizations.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var wordCount = checked((short)tokenizations[0].Length);
        var rows = await db.Database.SqlQueryRaw<ResolutionVariant>(
                ResolutionVariantsSql,
                new NpgsqlParameter<string>(
                    "tokenizations",
                    JsonSerializer.Serialize(tokenizations)),
                new NpgsqlParameter<Guid>("buildId", buildId),
                new NpgsqlParameter<short>("mode", (short)mode),
                new NpgsqlParameter<short>("wordCount", wordCount),
                new NpgsqlParameter<int>(
                    "candidateLimit",
                    PhraseSearchQueryLimits.MaximumResolutionCandidates))
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            results.TryAdd(TokenKey(row.ExactTokenIds), row);
        }
    }

    private static IReadOnlyList<ResolutionVariant> OrderCandidates(
        IEnumerable<ResolutionVariant> candidates) => candidates
            .OrderBy(candidate => candidate.WordCount)
            .ThenBy(candidate => candidate.ExactTokenIds, IntSequenceComparer.Instance)
            .Take(PhraseSearchQueryLimits.MaximumResolutionCandidates)
            .ToList();

    private static IReadOnlySet<string> CreatePossibleTexts(
        IReadOnlyList<string> segments,
        int maximumLength,
        CancellationToken cancellationToken)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        for (var start = 0; start < segments.Count; start++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var builder = new StringBuilder();
            for (var end = start; end < segments.Count; end++)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

    private static IReadOnlyList<TokenMatch>[] CreateTokenMatches(
        IReadOnlyList<string> segments,
        IReadOnlyDictionary<string, SearchTokenRow> tokensByText,
        int maximumLength,
        CancellationToken cancellationToken)
    {
        var matchesByStart = new IReadOnlyList<TokenMatch>[segments.Count];
        for (var start = 0; start < segments.Count; start++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = new List<TokenMatch>();
            var builder = new StringBuilder();
            for (var end = start; end < segments.Count; end++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.Append(segments[end]);
                if (builder.Length > maximumLength)
                {
                    break;
                }

                if (!tokensByText.TryGetValue(builder.ToString(), out var token))
                {
                    continue;
                }

                matches.Add(new TokenMatch(
                    end + 1,
                    checked((int)token.Id),
                    end - start));
            }

            matchesByStart[start] = matches;
        }

        return matchesByStart;
    }

    private static int[] Append(IReadOnlyList<int> values, int value) => [.. values, value];

    private static string TokenKey(IReadOnlyList<int> values) => string.Join(',', values);

    private sealed record SearchTokenRow(long Id, string SearchText);
    private sealed record TokenMatch(
        int NextSegmentIndex,
        int SearchTokenId,
        int JoinedBoundaryCount);
    private sealed record SearchState(
        int NextSegmentIndex,
        int JoinedBoundaryCount,
        int[] SearchTokenIds);

    private abstract record ResolutionSearchResult
    {
        private ResolutionSearchResult() { }

        internal sealed record Found(IReadOnlyList<ResolutionVariant> Candidates) : ResolutionSearchResult;
        internal sealed record TooComplex : ResolutionSearchResult;
    }

    private sealed class SearchStateOrderComparer : IComparer<SearchState>
    {
        internal static SearchStateOrderComparer Instance { get; } = new();

        public int Compare(SearchState? left, SearchState? right)
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

            var costComparison = left.JoinedBoundaryCount.CompareTo(right.JoinedBoundaryCount);
            if (costComparison != 0)
            {
                return costComparison;
            }

            var tokenComparison = IntSequenceComparer.Instance.Compare(
                left.SearchTokenIds,
                right.SearchTokenIds);
            return tokenComparison != 0
                ? tokenComparison
                : left.NextSegmentIndex.CompareTo(right.NextSegmentIndex);
        }
    }

    private sealed class SearchStateIdentityComparer : IEqualityComparer<SearchState>
    {
        internal static SearchStateIdentityComparer Instance { get; } = new();

        public bool Equals(SearchState? left, SearchState? right) =>
            ReferenceEquals(left, right)
            || left is not null
            && right is not null
            && left.NextSegmentIndex == right.NextSegmentIndex
            && left.JoinedBoundaryCount == right.JoinedBoundaryCount
            && left.SearchTokenIds.AsSpan().SequenceEqual(right.SearchTokenIds);

        public int GetHashCode(SearchState state)
        {
            var hash = new HashCode();
            hash.Add(state.NextSegmentIndex);
            hash.Add(state.JoinedBoundaryCount);
            foreach (var tokenId in state.SearchTokenIds)
            {
                hash.Add(tokenId);
            }

            return hash.ToHashCode();
        }
    }

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
