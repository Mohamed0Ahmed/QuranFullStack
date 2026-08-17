using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer
{
    internal async Task<IReadOnlyList<int>> AddInclusionsAsync(
        IReadOnlyList<AbwabDoorInclusion> inclusions,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (inclusions.Count == 0)
        {
            return [];
        }

        var targetDoorIds = inclusions.Select(inclusion => inclusion.TargetDoorId).Distinct().ToArray();
        if (targetDoorIds.Length != 1)
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }

        var now = DateTimeOffset.UtcNow;
        var contributionsByInclusionId = inclusions
            .OrderBy(inclusion => inclusion.Id)
            .ToDictionary(
                inclusion => inclusion.Id,
                inclusion => CreateContribution(inclusion, actorUserId, now));
        db.LinkingSourceContributions.AddRange(contributionsByInclusionId.Values);
        await SaveChangesAsync(cancellationToken);

        var addedUnitsByDoor = new Dictionary<int, List<long>>();
        var affectedAyahsByDoor = new Dictionary<int, HashSet<int>>();
        foreach (var inclusion in inclusions.OrderBy(inclusion => inclusion.Id))
        {
            var sourceUnitIds = await LoadLiveUnitIdsAsync(inclusion.SourceDoorId, cancellationToken);
            var clonedUnitIds = await CloneUnitsAsync(
                inclusion,
                contributionsByInclusionId[inclusion.Id],
                sourceUnitIds,
                actorUserId,
                now,
                affectedAyahsByDoor,
                cancellationToken);
            AddUnits(addedUnitsByDoor, inclusion.TargetDoorId, clonedUnitIds);
        }

        var traversal = await LoadActiveConsumerTraversalAsync(targetDoorIds[0], cancellationToken);
        foreach (var edge in traversal)
        {
            if (!addedUnitsByDoor.TryGetValue(edge.SourceDoorId, out var sourceUnitIds)
                || sourceUnitIds.Count == 0)
            {
                continue;
            }

            var inclusion = await db.AbwabDoorInclusions
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == edge.InclusionId && candidate.DeletedAtUtc == null,
                    cancellationToken);
            var contribution = await db.LinkingSourceContributions
                .SingleOrDefaultAsync(
                    candidate => candidate.DoorInclusionId == edge.InclusionId
                        && candidate.DeletedAtUtc == null,
                    cancellationToken);
            if (inclusion is null || contribution is null)
            {
                throw new AbwabDoorInclusionSynchronizationUnavailableException();
            }

            var clonedUnitIds = await CloneUnitsAsync(
                inclusion,
                contribution,
                sourceUnitIds,
                actorUserId,
                now,
                affectedAyahsByDoor,
                cancellationToken);
            AddUnits(addedUnitsByDoor, edge.TargetDoorId, clonedUnitIds);
        }

        foreach (var affected in affectedAyahsByDoor.OrderBy(entry => entry.Key))
        {
            await new RelationalDoorStateRebuilder(db).RebuildAsync(
                affected.Key,
                affected.Value,
                actorUserId,
                false,
                cancellationToken);
        }

        return affectedAyahsByDoor.Keys.Order().ToArray();
    }

    private async Task<IReadOnlyList<long>> CloneUnitsAsync(
        AbwabDoorInclusion inclusion,
        LinkingSourceContribution contribution,
        IReadOnlyCollection<long> sourceUnitIds,
        int actorUserId,
        DateTimeOffset now,
        IDictionary<int, HashSet<int>> affectedAyahsByDoor,
        CancellationToken cancellationToken)
    {
        if (sourceUnitIds.Count == 0)
        {
            return [];
        }

        var orderedSourceUnitIds = sourceUnitIds.Distinct().Order().ToArray();
        var snapshots = await AbwabDoorInclusionSourceSnapshot.LoadAsync(
            db,
            orderedSourceUnitIds,
            cancellationToken);
        if (snapshots.Count != orderedSourceUnitIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }

        var nextOrder = await db.LinkingSourceContributionUnits.AsNoTracking()
            .Where(mapping => mapping.SourceContributionId == contribution.Id)
            .Select(mapping => (int?)mapping.OrderValue)
            .MaxAsync(cancellationToken) ?? 0;
        var clonesBySourceUnitId = orderedSourceUnitIds.ToDictionary(
            sourceUnitId => sourceUnitId,
            sourceUnitId => CreateClone(inclusion, snapshots[sourceUnitId], actorUserId, now));
        db.LinkingUnits.AddRange(clonesBySourceUnitId.Values);
        await SaveChangesAsync(cancellationToken);

        var ayahsBySourceUnitAndAyah = new Dictionary<(long SourceUnitId, int AyahId), LinkingUnitAyah>();
        foreach (var sourceUnitId in orderedSourceUnitIds)
        {
            var snapshot = snapshots[sourceUnitId];
            var clone = clonesBySourceUnitId[sourceUnitId];
            foreach (var ayah in snapshot.Ayahs)
            {
                ayahsBySourceUnitAndAyah.Add(
                    (sourceUnitId, ayah.AyahId),
                    new LinkingUnitAyah
                    {
                        UnitId = clone.Id,
                        AyahId = ayah.AyahId,
                        OrderValue = ayah.OrderValue,
                    });
            }
        }

        db.LinkingUnitAyahs.AddRange(ayahsBySourceUnitAndAyah.Values);
        await SaveChangesAsync(cancellationToken);

        foreach (var sourceUnitId in orderedSourceUnitIds)
        {
            var snapshot = snapshots[sourceUnitId];
            foreach (var ayah in snapshot.Ayahs)
            {
                var cloneAyah = ayahsBySourceUnitAndAyah[(sourceUnitId, ayah.AyahId)];
                db.LinkingUnitAyahWords.AddRange(ayah.SelectedWordIds.Select(wordId =>
                    new LinkingUnitAyahWord
                    {
                        UnitAyahId = cloneAyah.Id,
                        QuranWordId = wordId,
                        AyahId = ayah.AyahId,
                    }));
                db.LinkingUnitAyahDescriptions.AddRange(ayah.Descriptions.Select((body, index) =>
                    new LinkingUnitAyahDescription
                    {
                        UnitAyahId = cloneAyah.Id,
                        OrderValue = index + 1,
                        Body = body,
                        CreatedAtUtc = now,
                        CreatedBy = actorUserId,
                        UpdatedAtUtc = now,
                        UpdatedBy = actorUserId,
                    }));
            }
        }

        db.LinkingSourceContributionUnits.AddRange(orderedSourceUnitIds.Select((sourceUnitId, index) =>
            new LinkingSourceContributionUnit
            {
                SourceContributionId = contribution.Id,
                UnitId = clonesBySourceUnitId[sourceUnitId].Id,
                OrderValue = nextOrder + index + 1,
            }));
        db.AbwabDoorInclusionUnitSyncs.AddRange(orderedSourceUnitIds.Select(sourceUnitId =>
            new AbwabDoorInclusionUnitSync
            {
                DoorInclusionId = inclusion.Id,
                SourceUnitId = sourceUnitId,
                TargetUnitId = clonesBySourceUnitId[sourceUnitId].Id,
                State = AbwabDoorInclusionSyncState.Active,
                SourceFingerprint = AbwabDoorInclusionFingerprint.Compute(snapshots[sourceUnitId]),
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
                UpdatedAtUtc = now,
                UpdatedBy = actorUserId,
            }));
        await SaveChangesAsync(cancellationToken);

        contribution.ResolvedAyahCount = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking()
                    on mapping.UnitId equals unitAyah.UnitId
                where mapping.SourceContributionId == contribution.Id
                select unitAyah.AyahId)
            .Distinct()
            .CountAsync(cancellationToken);
        contribution.ResolvedAtUtc = now;
        contribution.UpdatedAtUtc = now;
        contribution.UpdatedBy = actorUserId;
        await SaveChangesAsync(cancellationToken);

        if (!affectedAyahsByDoor.TryGetValue(inclusion.TargetDoorId, out var affectedAyahs))
        {
            affectedAyahs = [];
            affectedAyahsByDoor.Add(inclusion.TargetDoorId, affectedAyahs);
        }

        affectedAyahs.UnionWith(snapshots.Values.SelectMany(snapshot => snapshot.Ayahs).Select(ayah => ayah.AyahId));
        return orderedSourceUnitIds.Select(sourceUnitId => clonesBySourceUnitId[sourceUnitId].Id).ToArray();
    }

    private async Task<IReadOnlyList<long>> LoadLiveUnitIdsAsync(
        int doorId,
        CancellationToken cancellationToken) =>
        await db.LinkingUnits.AsNoTracking()
            .Where(unit => unit.DoorId == doorId)
            .Where(unit =>
                db.LinkingSourceContributionUnits.AsNoTracking()
                    .Where(mapping => mapping.UnitId == unit.Id)
                    .Join(
                        db.LinkingSourceContributions.AsNoTracking()
                            .Where(contribution =>
                                contribution.DoorId == doorId
                                && contribution.DeletedAtUtc == null),
                        mapping => mapping.SourceContributionId,
                        contribution => contribution.Id,
                        (_, _) => 1)
                    .Any())
            .OrderBy(unit => unit.Id)
            .Select(unit => unit.Id)
            .ToListAsync(cancellationToken);

    private static LinkingSourceContribution CreateContribution(
        AbwabDoorInclusion inclusion,
        int actorUserId,
        DateTimeOffset now)
    {
        var identity = $"door-inclusion|{inclusion.Id}";
        return new LinkingSourceContribution
        {
            DoorInclusionId = inclusion.Id,
            DoorId = inclusion.TargetDoorId,
            OrderValue = 1,
            ContributionMode = LinkingContributionMode.Automatic,
            SourceKind = LinkingSourceKind.DoorInclusion,
            SourceIdentity = identity,
            SourceIdentityHash = LinkingSourceIdentity.HashOf(identity),
            Label = $"door-inclusion:{inclusion.Id}",
            ScopeJson = "{\"schemaVersion\":1}",
            ResolvedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = actorUserId,
            UpdatedAtUtc = now,
            UpdatedBy = actorUserId,
        };
    }

    private static LinkingUnit CreateClone(
        AbwabDoorInclusion inclusion,
        AbwabDoorInclusionSourceSnapshot snapshot,
        int actorUserId,
        DateTimeOffset now)
    {
        var identity = $"door-inclusion-unit|{inclusion.Id}|{snapshot.UnitId}";
        return new LinkingUnit
        {
            DoorId = inclusion.TargetDoorId,
            Identity = identity,
            IdentityHash = LinkingSourceIdentity.HashOf(identity),
            IsGrouped = snapshot.IsGrouped,
            CreatedAtUtc = now,
            CreatedBy = actorUserId,
        };
    }

    private static void AddUnits(
        IDictionary<int, List<long>> addedUnitsByDoor,
        int doorId,
        IReadOnlyCollection<long> unitIds)
    {
        if (!addedUnitsByDoor.TryGetValue(doorId, out var addedUnitIds))
        {
            addedUnitIds = [];
            addedUnitsByDoor.Add(doorId, addedUnitIds);
        }

        addedUnitIds.AddRange(unitIds);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }
    }
}

internal sealed class AbwabDoorInclusionSynchronizationUnavailableException : Exception
{
}
