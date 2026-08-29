using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private PhraseContextSidePageDto CreateSidePage(
        PhraseContextSelection selection,
        BranchSideLoad loaded,
        PhraseContextSide side,
        int offset,
        int pageSize,
        ulong scope,
        int totalOccurrenceCount,
        IReadOnlyDictionary<int, string> tokenTexts)
    {
        var selected = side == PhraseContextSide.Previous ? selection.Previous : selection.Following;
        var selectedIds = selected?.SelectedExactTokenIds ?? [];
        var items = PinnedOptions(selection, side, loaded, tokenTexts)
            .Concat(loaded.Options)
            .Select(option => CreateBranchOption(selection, side, selectedIds, option))
            .ToList();
        var kind = side == PhraseContextSide.Previous
            ? PhraseCursorKind.PreviousBranches
            : PhraseCursorKind.FollowingBranches;
        return new PhraseContextSidePageDto(
            totalOccurrenceCount,
            loaded.BoundaryCount,
            loaded.TotalOptions,
            CreateNextCursor(
                selection.Resolution.BuildId,
                kind,
                offset,
                pageSize,
                loaded.CandidatePageCount,
                scope),
            items);
    }

    private static IReadOnlyList<int> BranchResponseExactTokenIds(PhraseContextSelection selection) => selection
        .Resolution.ExactTokenIds
        .Concat(selection.Previous?.SelectedExactTokenIds ?? [])
        .Concat(selection.Following?.SelectedExactTokenIds ?? [])
        .Concat(selection.PreviousAlternatives?.AlternativeExactTokenIds ?? [])
        .Concat(selection.FollowingAlternatives?.AlternativeExactTokenIds ?? [])
        .Distinct()
        .Order()
        .ToArray();

    private static IEnumerable<BranchOption> PinnedOptions(
        PhraseContextSelection selection,
        PhraseContextSide side,
        BranchSideLoad loaded,
        IReadOnlyDictionary<int, string> tokenTexts)
    {
        var alternativeTokenIds = side == PhraseContextSide.Previous
            ? selection.PreviousAlternatives?.AlternativeExactTokenIds
            : selection.FollowingAlternatives?.AlternativeExactTokenIds;
        if (alternativeTokenIds is null)
        {
            return [];
        }

        var existingById = loaded.PinnedOptions.ToDictionary(option => option.ExactTokenId!.Value);
        return alternativeTokenIds
            .Select(exactTokenId => existingById.GetValueOrDefault(exactTokenId)
                ?? new BranchOption(
                    exactTokenId,
                    tokenTexts[exactTokenId],
                    false,
                    0,
                    0))
            .OrderByDescending(option => option.PassesThroughCount)
            .ThenBy(option => option.ExactTokenId);
    }

    private PhraseContextBranchOptionDto CreateBranchOption(
        PhraseContextSelection selection,
        PhraseContextSide side,
        IReadOnlyList<int> selectedIds,
        BranchOption option)
    {
        var childIds = option.IsBoundary
            ? selectedIds
            : selectedIds.Append(option.ExactTokenId!.Value).ToArray();
        var path = new PhrasePathReference(
            selection.Resolution.BuildId,
            selection.Resolution.Mode,
            side,
            selection.Resolution.ExactTokenIds,
            childIds,
            option.IsBoundary);
        var boundaryKind = option.IsBoundary
            ? side == PhraseContextSide.Previous
                ? PhraseContextBoundaryKinds.AyahStart
                : PhraseContextBoundaryKinds.AyahEnd
            : null;
        var displayText = option.IsBoundary
            ? side == PhraseContextSide.Previous ? "بداية الآية" : "نهاية الآية"
            : option.DisplayText
                ?? throw new InvalidDataException("PhraseSearch context token branch has no display text.");
        return new PhraseContextBranchOptionDto(
            codec.EncodePath(path),
            option.ExactTokenId,
            displayText,
            boundaryKind,
            option.PassesThroughCount,
            option.SideEndsHereCount,
            IsAlternativeSelected(selection, side, option),
            CreateAlternativeToggleRef(selection, side, option));
    }

    private static bool IsAlternativeSelected(
        PhraseContextSelection selection,
        PhraseContextSide side,
        BranchOption option) => !option.IsBoundary
            && (side == PhraseContextSide.Previous
                ? selection.PreviousAlternatives
                : selection.FollowingAlternatives)
            ?.AlternativeExactTokenIds.Contains(option.ExactTokenId!.Value) == true;

    private string? CreateAlternativeToggleRef(
        PhraseContextSelection selection,
        PhraseContextSide side,
        BranchOption option)
    {
        if (option.IsBoundary)
        {
            return null;
        }

        var alternatives = side == PhraseContextSide.Previous
            ? selection.PreviousAlternatives
            : selection.FollowingAlternatives;
        var exactTokenId = option.ExactTokenId!.Value;
        var alternativeTokenIds = alternatives?.AlternativeExactTokenIds ?? [];
        var nextAlternativeTokenIds = alternativeTokenIds.Contains(exactTokenId)
            ? alternativeTokenIds.Where(tokenId => tokenId != exactTokenId).ToArray()
            : alternativeTokenIds.Append(exactTokenId).ToArray();
        if (nextAlternativeTokenIds.Length == 0)
        {
            return null;
        }

        var committedPath = side == PhraseContextSide.Previous
            ? selection.Previous
            : selection.Following;
        return codec.EncodeAlternative(new PhraseContextAlternativeReference(
            selection.Resolution.BuildId,
            selection.Resolution.Mode,
            side,
            selection.Resolution.ExactTokenIds,
            committedPath?.SelectedExactTokenIds ?? [],
            nextAlternativeTokenIds));
    }
}
