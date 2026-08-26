using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextBranchesResponse>> GetBranchesAsync(
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.BuildChanged();
        }

        var loaded = await LoadPopulationAsync(snapshot, selection.Resolution, cancellationToken);
        var filtered = ApplySelection(loaded.Occurrences, selection);
        if (!loaded.QueryExists || filtered.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference();
        }

        var scope = codec.ComputeScope(selection);
        var previous = CreateSidePage(
            selection,
            filtered,
            PhraseContextSide.Previous,
            paging.PreviousOffset,
            paging.PreviousPageSize,
            scope);
        var following = CreateSidePage(
            selection,
            filtered,
            PhraseContextSide.Following,
            paging.FollowingOffset,
            paging.FollowingPageSize,
            scope);
        var representative = filtered[0];
        var bothBoundariesFixed = selection.Previous?.EndsAtBoundary == true
            && selection.Following?.EndsAtBoundary == true;
        var response = new PhraseContextBranchesResponse(
            snapshot.ActiveBuildId,
            CreateResolvedQuery(selection.Resolution, representative),
            CreateSelectedPath(selection.Resolution, selection.Previous, PhraseContextSide.Previous, representative),
            CreateSelectedPath(selection.Resolution, selection.Following, PhraseContextSide.Following, representative),
            previous,
            following,
            filtered.Count,
            bothBoundariesFixed ? filtered.Count : null);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Success(response);
    }

    private PhraseContextSidePageDto CreateSidePage(
        PhraseContextSelection selection,
        IReadOnlyList<ContextOccurrence> population,
        PhraseContextSide side,
        int offset,
        int pageSize,
        ulong scope)
    {
        var selected = side == PhraseContextSide.Previous ? selection.Previous : selection.Following;
        var selectedIds = selected?.SelectedExactTokenIds ?? [];
        var boundaryCount = population.Count(occurrence =>
            IsAtBoundary(occurrence, side, selectedIds.Count));
        var options = new List<BranchOption>();
        if (selected?.EndsAtBoundary != true)
        {
            options.AddRange(population
                .Where(occurrence => !IsAtBoundary(occurrence, side, selectedIds.Count))
                .GroupBy(occurrence => GetPathWord(occurrence, side, selectedIds.Count).ExactTokenId)
                .Select(group =>
                {
                    var representative = GetPathWord(group.First(), side, selectedIds.Count);
                    var childIds = selectedIds.Append(group.Key).ToArray();
                    var childBoundaryCount = group.Count(occurrence =>
                        IsAtBoundary(occurrence, side, childIds.Length));
                    var path = new PhrasePathReference(
                        selection.Resolution.BuildId,
                        selection.Resolution.Mode,
                        side,
                        selection.Resolution.ExactTokenIds,
                        childIds,
                        false);
                    return new BranchOption(
                        codec.EncodePath(path),
                        group.Key,
                        representative.TextUthmani,
                        null,
                        group.LongCount(),
                        childBoundaryCount);
                }));

            if (boundaryCount > 0)
            {
                var boundaryPath = new PhrasePathReference(
                    selection.Resolution.BuildId,
                    selection.Resolution.Mode,
                    side,
                    selection.Resolution.ExactTokenIds,
                    selectedIds,
                    true);
                options.Add(new BranchOption(
                    codec.EncodePath(boundaryPath),
                    null,
                    side == PhraseContextSide.Previous ? "بداية الآية" : "نهاية الآية",
                    side == PhraseContextSide.Previous
                        ? PhraseContextBoundaryKinds.AyahStart
                        : PhraseContextBoundaryKinds.AyahEnd,
                    boundaryCount,
                    boundaryCount));
            }
        }

        var ordered = options
            .OrderByDescending(option => option.PassesThroughCount)
            .ThenBy(option => option.ExactTokenId ?? int.MinValue)
            .ToList();
        var items = ordered
            .Skip(offset)
            .Take(pageSize)
            .Select(option => new PhraseContextBranchOptionDto(
                option.SelectionRef,
                option.ExactTokenId,
                option.DisplayText,
                option.BoundaryKind,
                option.PassesThroughCount,
                option.SideEndsHereCount))
            .ToList();
        var kind = side == PhraseContextSide.Previous
            ? PhraseCursorKind.PreviousBranches
            : PhraseCursorKind.FollowingBranches;
        return new PhraseContextSidePageDto(
            population.Count,
            boundaryCount,
            ordered.Count,
            CreateNextCursor(selection.Resolution.BuildId, kind, offset, pageSize, ordered.Count, scope),
            items);
    }

    private sealed record BranchOption(
        string SelectionRef,
        int? ExactTokenId,
        string DisplayText,
        string? BoundaryKind,
        long PassesThroughCount,
        long SideEndsHereCount);
}
