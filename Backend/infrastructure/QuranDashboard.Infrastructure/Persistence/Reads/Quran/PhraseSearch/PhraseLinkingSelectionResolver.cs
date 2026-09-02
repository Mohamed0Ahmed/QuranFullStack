using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

internal static class PhraseLinkingSelectionResolver
{
    internal static IReadOnlyList<int>? ResolveAyahIds(
        PhraseLinkingAyahSelection selection,
        IReadOnlyList<int> orderedEligibleAyahIds)
    {
        var eligibleAyahIds = orderedEligibleAyahIds.ToHashSet();
        if (eligibleAyahIds.Count != orderedEligibleAyahIds.Count
            || eligibleAyahIds.Any(ayahId => ayahId <= 0))
        {
            throw new InvalidDataException("PhraseSearch linking selection contains an invalid ayah population.");
        }

        var overrideAyahIds = selection.OverrideAyahIds.ToHashSet();
        if (!overrideAyahIds.IsSubsetOf(eligibleAyahIds))
        {
            return null;
        }

        var selectedAyahIds = selection.Mode switch
        {
            PhraseLinkingAyahSelectionMode.Only => orderedEligibleAyahIds
                .Where(overrideAyahIds.Contains)
                .ToList(),
            PhraseLinkingAyahSelectionMode.AllExcept => orderedEligibleAyahIds
                .Where(ayahId => !overrideAyahIds.Contains(ayahId))
                .ToList(),
            _ => throw new InvalidDataException("PhraseSearch linking selection contains an invalid mode."),
        };
        return selectedAyahIds.Count == 0 ? null : selectedAyahIds;
    }

    internal static IReadOnlyList<int> OrderSelectedWords(
        IReadOnlyList<int> orderedAuthoritativeWordIds,
        IReadOnlyCollection<int> selectedWordIds)
    {
        var authoritativeWordIds = orderedAuthoritativeWordIds.ToHashSet();
        var selectedWordIdSet = selectedWordIds.ToHashSet();
        if (authoritativeWordIds.Count == 0
            || authoritativeWordIds.Count != orderedAuthoritativeWordIds.Count
            || authoritativeWordIds.Any(wordId => wordId <= 0)
            || selectedWordIdSet.Count == 0
            || selectedWordIdSet.Count != selectedWordIds.Count
            || selectedWordIdSet.Any(wordId => wordId <= 0)
            || !selectedWordIdSet.IsSubsetOf(authoritativeWordIds))
        {
            throw new InvalidDataException("PhraseSearch linking selection contains an invalid Quran word.");
        }

        return orderedAuthoritativeWordIds
            .Where(selectedWordIdSet.Contains)
            .ToList();
    }
}
