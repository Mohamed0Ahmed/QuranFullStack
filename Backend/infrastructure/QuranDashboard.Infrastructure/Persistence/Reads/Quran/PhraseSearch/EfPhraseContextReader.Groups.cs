using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextGroupsResponse>> GetGroupsAsync(
        PhraseContextSelection selection,
        PhraseCursorPage paging,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.BuildChanged();
        }

        var loaded = await LoadPopulationAsync(snapshot, selection.Resolution, cancellationToken);
        var filtered = ApplySelection(loaded.Occurrences, selection);
        if (!loaded.QueryExists || filtered.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.InvalidReference();
        }

        var groups = filtered
            .Select(occurrence => new FullContextOccurrence(
                occurrence,
                FullPathIds(occurrence, PhraseContextSide.Previous),
                FullPathIds(occurrence, PhraseContextSide.Following)))
            .GroupBy(item => ContextKey(item.PreviousIds, item.FollowingIds))
            .Select(group => new FullContextGroup(
                group.First().PreviousIds,
                group.First().FollowingIds,
                group.Select(item => item.Occurrence).ToList()))
            .OrderByDescending(group => group.Occurrences.Count)
            .ThenBy(group => group.PreviousIds, ContextIdComparer.Instance)
            .ThenBy(group => group.FollowingIds, ContextIdComparer.Instance)
            .ToList();
        var items = groups
            .Skip(paging.Offset)
            .Take(paging.PageSize)
            .Select(group => CreateGroup(selection.Resolution, group))
            .ToList();
        var representative = filtered[0];
        var scope = codec.ComputeScope(selection);
        var response = new PhraseContextGroupsResponse(
            snapshot.ActiveBuildId,
            CreateResolvedQuery(selection.Resolution, representative),
            CreateSelectedPath(selection.Resolution, selection.Previous, PhraseContextSide.Previous, representative),
            CreateSelectedPath(selection.Resolution, selection.Following, PhraseContextSide.Following, representative),
            groups.Count,
            CreateNextCursor(
                snapshot.ActiveBuildId,
                PhraseCursorKind.ContextGroups,
                paging.Offset,
                paging.PageSize,
                groups.Count,
                scope),
            items);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseContextGroupsResponse>.Success(response);
    }

    private PhraseFullContextGroupDto CreateGroup(
        PhraseResolutionReference resolution,
        FullContextGroup group)
    {
        var representative = group.Occurrences[0];
        var context = new PhraseFullContextReference(
            resolution.BuildId,
            resolution.Mode,
            resolution.ExactTokenIds,
            group.PreviousIds,
            group.FollowingIds);
        return new PhraseFullContextGroupDto(
            codec.EncodeFullContext(context),
            FullPathTokens(representative, PhraseContextSide.Previous),
            CreateResolvedQuery(resolution, representative).Tokens,
            FullPathTokens(representative, PhraseContextSide.Following),
            group.Occurrences.Count);
    }

    private static string ContextKey(
        IReadOnlyList<int> previous,
        IReadOnlyList<int> following) => $"{string.Join(',', previous)}|{string.Join(',', following)}";

    private sealed record FullContextOccurrence(
        ContextOccurrence Occurrence,
        IReadOnlyList<int> PreviousIds,
        IReadOnlyList<int> FollowingIds);

    private sealed record FullContextGroup(
        IReadOnlyList<int> PreviousIds,
        IReadOnlyList<int> FollowingIds,
        IReadOnlyList<ContextOccurrence> Occurrences);

    private sealed class ContextIdComparer : IComparer<IReadOnlyList<int>>
    {
        internal static ContextIdComparer Instance { get; } = new();

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
    }
}
