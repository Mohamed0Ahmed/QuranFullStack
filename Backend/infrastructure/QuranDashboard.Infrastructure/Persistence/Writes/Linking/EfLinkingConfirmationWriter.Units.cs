using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<IReadOnlyDictionary<string, long>> EnsureUnitsAsync(
        int doorId,
        int actorUserId,
        ConfirmationWorkset workset,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unitIds = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var batch in BatchesOf(workset.Units))
        {
            var hashes = batch.Select(unit => unit.IdentityHash).ToList();
            var candidates = await db.LinkingUnits
                .AsNoTracking()
                .Where(unit => unit.DoorId == doorId && hashes.Contains(unit.IdentityHash))
                .ToListAsync(cancellationToken);
            var requestedByHash = batch.ToDictionary(unit => unit.IdentityHashKey, StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                var hashKey = Convert.ToHexString(candidate.IdentityHash);
                if (!requestedByHash.TryGetValue(hashKey, out var requested))
                {
                    continue;
                }

                if (!string.Equals(candidate.Identity, requested.Intent.Identity, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A linking unit identity hash collision was detected.");
                }

                unitIds.Add(requested.Intent.Identity, candidate.Id);
            }
        }

        var missing = workset.Units
            .Where(unit => !unitIds.ContainsKey(unit.Intent.Identity))
            .ToList();

        foreach (var batch in BatchesOf(missing))
        {
            var entities = batch.Select(unit => new LinkingUnit
            {
                DoorId = doorId,
                Identity = unit.Intent.Identity,
                IdentityHash = unit.IdentityHash,
                IsGrouped = unit.Intent.IsGrouped,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            }).ToList();

            db.LinkingUnits.AddRange(entities);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);

            for (var index = 0; index < batch.Length; index++)
            {
                unitIds.Add(batch[index].Intent.Identity, entities[index].Id);
            }

            DetachRange(entities);
        }

        await InsertUnitChildrenAsync(missing, unitIds, actorUserId, now, cancellationToken);
        return unitIds;
    }

    private async Task InsertUnitChildrenAsync(
        IReadOnlyList<WorksetUnit> units,
        IReadOnlyDictionary<string, long> unitIds,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var ayahs = units.SelectMany(unit => unit.Intent.Ayahs.Select((ayah, index) => new PendingUnitAyah(
            new LinkingUnitAyah
            {
                UnitId = unitIds[unit.Intent.Identity],
                AyahId = ayah.AyahId,
                OrderValue = index + 1,
            },
            ayah)));

        foreach (var batch in BatchesOf(ayahs))
        {
            var entities = batch.Select(item => item.Entity).ToList();
            db.LinkingUnitAyahs.AddRange(entities);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);

            await InsertUnitWordsAsync(batch, cancellationToken);
            await InsertUnitDescriptionsAsync(batch, actorUserId, now, cancellationToken);
            DetachRange(entities);
        }
    }

    private async Task InsertUnitWordsAsync(
        IReadOnlyList<PendingUnitAyah> ayahs,
        CancellationToken cancellationToken)
    {
        var words = ayahs.SelectMany(item => item.Intent.WordIds.Select(wordId => new LinkingUnitAyahWord
        {
            UnitAyahId = item.Entity.Id,
            QuranWordId = wordId,
            AyahId = item.Intent.AyahId,
        }));

        foreach (var batch in BatchesOf(words))
        {
            db.LinkingUnitAyahWords.AddRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(batch);
        }
    }

    private async Task InsertUnitDescriptionsAsync(
        IReadOnlyList<PendingUnitAyah> ayahs,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var descriptions = ayahs.SelectMany(item => item.Intent.Descriptions.Select((body, index) =>
            new LinkingUnitAyahDescription
            {
                UnitAyahId = item.Entity.Id,
                OrderValue = index + 1,
                Body = body,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
                UpdatedAtUtc = now,
                UpdatedBy = actorUserId,
            }));

        foreach (var batch in BatchesOf(descriptions))
        {
            db.LinkingUnitAyahDescriptions.AddRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(batch);
        }
    }

    private async Task RemoveNewlyOrphanedUnitsAsync(
        IReadOnlySet<long> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var batch in BatchesOf(candidates))
        {
            var orphanIds = await db.LinkingUnits
                .Where(unit => batch.Contains(unit.Id))
                .Where(unit => !db.LinkingSourceContributionUnits.Any(link => link.UnitId == unit.Id))
                .Select(unit => unit.Id)
                .ToListAsync(cancellationToken);

            if (orphanIds.Count == 0)
            {
                continue;
            }

            await db.LinkingUnitAyahDescriptions
                .Where(description => db.LinkingUnitAyahs.Any(ayah =>
                    ayah.Id == description.UnitAyahId && orphanIds.Contains(ayah.UnitId)))
                .ExecuteDeleteAsync(cancellationToken);
            await db.LinkingUnitAyahWords
                .Where(word => db.LinkingUnitAyahs.Any(ayah =>
                    ayah.Id == word.UnitAyahId && orphanIds.Contains(ayah.UnitId)))
                .ExecuteDeleteAsync(cancellationToken);
            await db.LinkingUnitAyahs
                .Where(ayah => orphanIds.Contains(ayah.UnitId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.LinkingUnits
                .Where(unit => orphanIds.Contains(unit.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private sealed record PendingUnitAyah(
        LinkingUnitAyah Entity,
        LinkingOperationAyahIntent Intent);
}
