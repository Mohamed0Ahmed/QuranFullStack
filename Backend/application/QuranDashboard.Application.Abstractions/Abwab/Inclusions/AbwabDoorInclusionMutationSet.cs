namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public sealed record AbwabDoorInclusionUnitReplacement(long PreviousUnitId, long CurrentUnitId);

public sealed class AbwabDoorInclusionMutationSet
{
    private AbwabDoorInclusionMutationSet(
        IReadOnlyList<long> addedUnitIds,
        IReadOnlyList<long> editedUnitIds,
        IReadOnlyList<long> deletedUnitIds,
        IReadOnlyList<AbwabDoorInclusionUnitReplacement> replacements)
    {
        AddedUnitIds = addedUnitIds;
        EditedUnitIds = editedUnitIds;
        DeletedUnitIds = deletedUnitIds;
        Replacements = replacements;
    }

    public IReadOnlyList<long> AddedUnitIds { get; }
    public IReadOnlyList<long> EditedUnitIds { get; }
    public IReadOnlyList<long> DeletedUnitIds { get; }
    public IReadOnlyList<AbwabDoorInclusionUnitReplacement> Replacements { get; }

    public bool IsEmpty =>
        AddedUnitIds.Count == 0
        && EditedUnitIds.Count == 0
        && DeletedUnitIds.Count == 0
        && Replacements.Count == 0;

    public static AbwabDoorInclusionMutationSet Create(
        IEnumerable<long> addedUnitIds,
        IEnumerable<long> editedUnitIds,
        IEnumerable<long> deletedUnitIds,
        IEnumerable<AbwabDoorInclusionUnitReplacement> replacements)
    {
        ArgumentNullException.ThrowIfNull(addedUnitIds);
        ArgumentNullException.ThrowIfNull(editedUnitIds);
        ArgumentNullException.ThrowIfNull(deletedUnitIds);
        ArgumentNullException.ThrowIfNull(replacements);

        var added = NormalizeIds(addedUnitIds, nameof(addedUnitIds));
        var edited = NormalizeIds(editedUnitIds, nameof(editedUnitIds));
        var deleted = NormalizeIds(deletedUnitIds, nameof(deletedUnitIds));
        var replacementList = replacements.ToArray();

        ValidateReplacementBijection(replacementList);
        ValidateDisjointOccurrenceOwnership(added, edited, deleted, replacementList);

        return new AbwabDoorInclusionMutationSet(added, edited, deleted, replacementList);
    }

    private static long[] NormalizeIds(IEnumerable<long> ids, string parameterName)
    {
        var normalized = ids.Order().ToArray();
        if (normalized.Any(id => id <= 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Unit IDs must be positive.");
        }

        if (normalized.Length != normalized.Distinct().Count())
        {
            throw new ArgumentException("A unit ID cannot occur more than once in one mutation category.", parameterName);
        }

        return normalized;
    }

    private static void ValidateReplacementBijection(
        IReadOnlyList<AbwabDoorInclusionUnitReplacement> replacements)
    {
        if (replacements.Any(replacement =>
                replacement.PreviousUnitId <= 0
                || replacement.CurrentUnitId <= 0
                || replacement.PreviousUnitId == replacement.CurrentUnitId))
        {
            throw new ArgumentException(
                "Replacement pairs require distinct positive previous and current unit IDs.",
                nameof(replacements));
        }

        if (replacements.Select(replacement => replacement.PreviousUnitId).Distinct().Count() != replacements.Count
            || replacements.Select(replacement => replacement.CurrentUnitId).Distinct().Count() != replacements.Count)
        {
            throw new ArgumentException(
                "Replacement pairs must form a deterministic one-to-one mapping.",
                nameof(replacements));
        }
    }

    private static void ValidateDisjointOccurrenceOwnership(
        IReadOnlyList<long> added,
        IReadOnlyList<long> edited,
        IReadOnlyList<long> deleted,
        IReadOnlyList<AbwabDoorInclusionUnitReplacement> replacements)
    {
        var occurrences = new HashSet<long>();
        if (!AddAll(occurrences, added)
            || !AddAll(occurrences, edited)
            || !AddAll(occurrences, deleted)
            || !AddAll(occurrences, replacements.Select(replacement => replacement.PreviousUnitId))
            || !AddAll(occurrences, replacements.Select(replacement => replacement.CurrentUnitId)))
        {
            throw new ArgumentException(
                "A unit ID must belong to exactly one mutation category or replacement position.");
        }
    }

    private static bool AddAll(HashSet<long> destination, IEnumerable<long> values)
    {
        foreach (var value in values)
        {
            if (!destination.Add(value))
            {
                return false;
            }
        }

        return true;
    }
}
