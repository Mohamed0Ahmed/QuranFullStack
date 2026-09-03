namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class AbwabDoorInclusionReconciler
{
    private async Task ReconcileSourceUnitReplacementsAsync(
        int inclusionId,
        IReadOnlyList<AbwabDoorInclusionUnitReplacement> replacements,
        CancellationToken cancellationToken)
    {
        if (replacements.Count == 0)
        {
            return;
        }

        var previousUnitIds = replacements.Select(replacement => replacement.PreviousUnitId).ToArray();
        var currentUnitIds = replacements.Select(replacement => replacement.CurrentUnitId).ToArray();
        var involvedUnitIds = previousUnitIds.Concat(currentUnitIds).ToArray();
        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.DoorInclusionId == inclusionId
                && involvedUnitIds.Contains(sync.SourceUnitId))
            .OrderBy(sync => sync.SourceUnitId)
            .ToListAsync(cancellationToken);
        var syncsBySourceUnitId = syncs.ToDictionary(sync => sync.SourceUnitId);
        if (previousUnitIds.Any(previousUnitId => !syncsBySourceUnitId.ContainsKey(previousUnitId))
            || currentUnitIds.Any(syncsBySourceUnitId.ContainsKey))
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        foreach (var replacement in replacements.OrderBy(replacement => replacement.PreviousUnitId))
        {
            syncsBySourceUnitId[replacement.PreviousUnitId].SourceUnitId = replacement.CurrentUnitId;
        }

        await SaveChangesAsync(cancellationToken);
    }
}
