using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

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

        var cacheKey = PhraseSearchCacheKeys.ContextBranches(selection, paging);
        if (cache.TryGet(cacheKey, out PhraseContextBranchesResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Success(cached);
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference();
        }

        var loaded = await ReadBranchesAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            paging,
            cancellationToken);
        var exactTokenIds = BranchResponseExactTokenIds(selection);
        var tokenTexts = await LoadExactTokenTextsAsync(
            selection.Resolution.Mode,
            exactTokenIds,
            cancellationToken);
        if (tokenTexts.Count != exactTokenIds.Count)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference();
        }

        var scope = codec.ComputeScope(selection);
        var previous = CreateSidePage(
            selection,
            loaded.Previous,
            PhraseContextSide.Previous,
            paging.PreviousOffset,
            paging.PreviousPageSize,
            scope,
            loaded.TotalOccurrenceCount,
            tokenTexts);
        var following = CreateSidePage(
            selection,
            loaded.Following,
            PhraseContextSide.Following,
            paging.FollowingOffset,
            paging.FollowingPageSize,
            scope,
            loaded.TotalOccurrenceCount,
            tokenTexts);
        var bothBoundariesFixed = selection.Previous?.EndsAtBoundary == true
            && selection.Following?.EndsAtBoundary == true;
        var response = new PhraseContextBranchesResponse(
            snapshot.ActiveBuildId,
            CreateResolvedQuery(selection.Resolution, tokenTexts),
            CreateSelectedPath(selection.Previous, PhraseContextSide.Previous, tokenTexts),
            CreateSelectedPath(selection.Following, PhraseContextSide.Following, tokenTexts),
            previous,
            following,
            loaded.TotalOccurrenceCount,
            bothBoundariesFixed ? loaded.TotalOccurrenceCount : null);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(
            cacheKey,
            response,
            PhraseSearchCacheKeys.PageWeight(
                paging.PreviousPageSize + paging.FollowingPageSize));
        return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Success(response);
    }

    private async Task<ContextBranchLoad> ReadBranchesAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            ContextBranchesWithAlternativesSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("previous_offset", NpgsqlDbType.Bigint, (long)paging.PreviousOffset);
        command.Parameters.AddWithValue("previous_page_size", paging.PreviousPageSize);
        command.Parameters.AddWithValue("following_offset", NpgsqlDbType.Bigint, (long)paging.FollowingOffset);
        command.Parameters.AddWithValue("following_page_size", paging.FollowingPageSize);

        var totalOccurrenceCount = 0;
        var hasInvalidExactIdentity = false;
        ContextOccurrenceRow? representative = null;
        var sides = new Dictionary<PhraseContextSide, MutableBranchSideLoad>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hasInvalidExactIdentity = reader.GetBoolean(0);
            totalOccurrenceCount = reader.GetInt32(1);
            if (hasInvalidExactIdentity)
            {
                continue;
            }

            if (!reader.IsDBNull(2))
            {
                representative ??= ReadOccurrenceRow(reader, 2);
            }

            var side = (PhraseContextSide)reader.GetInt16(12);
            if (!sides.TryGetValue(side, out var sideLoad))
            {
                sideLoad = new MutableBranchSideLoad(
                    reader.GetInt64(13),
                    reader.GetInt32(14),
                    reader.GetInt32(15),
                    [],
                    []);
                sides.Add(side, sideLoad);
            }

            if (reader.IsDBNull(16))
            {
                continue;
            }

            var isPinned = reader.GetBoolean(16);
            int? exactTokenId = reader.IsDBNull(18) ? null : reader.GetInt32(18);
            string? displayText = reader.IsDBNull(19) ? null : reader.GetString(19);
            var isBoundary = reader.GetBoolean(20);
            if (!isBoundary && (exactTokenId is null || displayText is null))
            {
                throw new InvalidDataException("PhraseSearch context token branch has no attested source word.");
            }

            var option = new BranchOption(
                exactTokenId,
                displayText,
                isBoundary,
                reader.GetInt64(21),
                reader.GetInt64(22));
            (isPinned ? sideLoad.PinnedOptions : sideLoad.Options).Add(option);
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        if (!sides.TryGetValue(PhraseContextSide.Previous, out var previous)
            || !sides.TryGetValue(PhraseContextSide.Following, out var following))
        {
            throw new InvalidDataException("PhraseSearch context branch query did not return both sides.");
        }

        return new ContextBranchLoad(
            totalOccurrenceCount,
            representative,
            previous.ToImmutable(),
            following.ToImmutable());
    }

    private sealed record ContextBranchLoad(
        int TotalOccurrenceCount,
        ContextOccurrenceRow? Representative,
        BranchSideLoad Previous,
        BranchSideLoad Following);

    private sealed record BranchSideLoad(
        long BoundaryCount,
        int TotalOptions,
        int CandidatePageCount,
        IReadOnlyList<BranchOption> PinnedOptions,
        IReadOnlyList<BranchOption> Options);

    private sealed record BranchOption(
        int? ExactTokenId,
        string? DisplayText,
        bool IsBoundary,
        long PassesThroughCount,
        long SideEndsHereCount);

    private sealed record MutableBranchSideLoad(
        long BoundaryCount,
        int TotalOptions,
        int CandidatePageCount,
        List<BranchOption> PinnedOptions,
        List<BranchOption> Options)
    {
        internal BranchSideLoad ToImmutable() => new(
            BoundaryCount,
            TotalOptions,
            CandidatePageCount,
            PinnedOptions,
            Options);
    }
}
