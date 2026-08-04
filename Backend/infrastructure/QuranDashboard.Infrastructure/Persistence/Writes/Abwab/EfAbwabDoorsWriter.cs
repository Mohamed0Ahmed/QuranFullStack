using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal sealed class EfAbwabDoorsWriter(QuranDashboardDbContext db) : IAbwabDoorsWriter
{
    public async Task<AbwabDoorDto> CreateAsync(
        int? sectionId,
        int? parentId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        var resolvedSectionId = await ResolveCreateSectionAsync(parentId, sectionId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var nextOrder = await db.AbwabDoors.CountAsync(
            d => d.SectionId == resolvedSectionId && d.ParentId == parentId && d.DeletedAtUtc == null, cancellationToken) + 1;

        var door = new AbwabDoor
        {
            SectionId = resolvedSectionId,
            ParentId = parentId,
            Name = name,
            Description = description,
            RepresentativeAyahText = representativeAyahText,
            OrderValue = nextOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AbwabDoors.Add(door);

        if (parentId is null)
        {
            await MaintainGlobalOrderAsync(new HashSet<int>(), [door], cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SaveTranslatingWriteExceptionsAsync(name, cancellationToken);

        await ReplaceAliasesAsync(door.Id, aliases, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    public async Task<AbwabDoorDto?> EditAsync(
        int id,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAtUtc == null, cancellationToken);
        if (door is null)
        {
            return null;
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var now = DateTimeOffset.UtcNow;
        door.Name = name;
        door.Description = description;
        door.RepresentativeAyahText = representativeAyahText;
        door.UpdatedAtUtc = now;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SaveTranslatingWriteExceptionsAsync(name, cancellationToken);

        await ReplaceAliasesAsync(id, aliases, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    public async Task<AbwabDoorDto?> MoveAsync(
        int id,
        int? targetSectionId,
        int? targetParentId,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAtUtc == null, cancellationToken);
        if (door is null)
        {
            return null;
        }

        var resolvedSectionId = await ResolveTargetSectionAsync(targetSectionId, targetParentId, cancellationToken);
        var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);
        if (targetParentId.HasValue)
        {
            EnsureNotCycle(childrenByParent, id, targetParentId.Value);
        }

        var oldSectionId = door.SectionId;
        var oldParentId = door.ParentId;
        var scopeIsUnchanged = oldSectionId == resolvedSectionId && oldParentId == targetParentId;

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var destinationSiblings = await db.AbwabDoors
            .Where(d => d.SectionId == resolvedSectionId && d.ParentId == targetParentId
                        && d.DeletedAtUtc == null && d.Id != id)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        door.SectionId = resolvedSectionId;
        door.ParentId = targetParentId;
        door.UpdatedAtUtc = now;

        if (oldSectionId != resolvedSectionId)
        {
            await CascadeSectionToDescendantsAsync(childrenByParent, [id], resolvedSectionId, now, cancellationToken);
        }

        if (!scopeIsUnchanged)
        {
            await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);
        }

        Resequence(destinationSiblings.Append(door));

        if (oldParentId is null && targetParentId is not null)
        {
            door.GlobalOrderValue = null;
            await MaintainGlobalOrderAsync(new HashSet<int> { id }, [], cancellationToken);
        }
        else if (oldParentId is not null && targetParentId is null)
        {
            await MaintainGlobalOrderAsync(new HashSet<int>(), [door], cancellationToken);
        }

        await SaveTranslatingWriteExceptionsAsync(door.Name, cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    public async Task<AbwabDoorDto?> ReorderAsync(int id, int position, AbwabReorderScope scope, uint expectedVersion, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAtUtc == null, cancellationToken);
        if (door is null)
        {
            return null;
        }

        if (scope == AbwabReorderScope.Global)
        {
            if (door.ParentId.HasValue)
            {
                throw new AbwabScopeNotApplicableException();
            }

            var liveRoots = await db.AbwabDoors
                .Where(d => d.ParentId == null && d.DeletedAtUtc == null)
                .OrderBy(d => d.GlobalOrderValue).ThenBy(d => d.Id)
                .ToListAsync(cancellationToken);

            return await ReorderWithinAsync(door, position, expectedVersion, liveRoots, ResequenceGlobal, cancellationToken);
        }

        var siblings = await db.AbwabDoors
            .Where(d => d.SectionId == door.SectionId && d.ParentId == door.ParentId && d.DeletedAtUtc == null)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        return await ReorderWithinAsync(door, position, expectedVersion, siblings, Resequence, cancellationToken);
    }

    private async Task<AbwabDoorDto> ReorderWithinAsync(
        AbwabDoor door,
        int position,
        uint expectedVersion,
        List<AbwabDoor> orderedSet,
        Action<IEnumerable<AbwabDoor>> resequence,
        CancellationToken cancellationToken)
    {
        if (position < 1 || position > orderedSet.Count)
        {
            throw new AbwabInvalidPositionException();
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        orderedSet.RemoveAll(d => d.Id == door.Id);
        orderedSet.Insert(position - 1, door);
        resequence(orderedSet);

        door.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    public async Task<IReadOnlyList<AbwabDoorDto>> BulkMoveAsync(
        IReadOnlyList<AbwabBulkDoorRef> doors,
        int? targetSectionId,
        int? targetParentId,
        CancellationToken cancellationToken)
    {
        var ids = doors.Select(d => d.DoorId).ToList();
        var loaded = await db.AbwabDoors
            .Where(d => ids.Contains(d.Id) && d.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (loaded.Count != ids.Distinct().Count())
        {
            throw new AbwabNotFoundException();
        }

        var resolvedSectionId = await ResolveTargetSectionAsync(targetSectionId, targetParentId, cancellationToken);
        var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);
        if (targetParentId.HasValue)
        {
            foreach (var door in loaded)
            {
                EnsureNotCycle(childrenByParent, door.Id, targetParentId.Value);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var movedIds = loaded.Select(d => d.Id).ToHashSet();
        var sectionChanged = loaded.Any(d => d.SectionId != resolvedSectionId);

        var existingDestinationSiblings = await db.AbwabDoors
            .Where(d => d.SectionId == resolvedSectionId && d.ParentId == targetParentId
                        && d.DeletedAtUtc == null && !movedIds.Contains(d.Id))
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        var oldScopes = loaded.Select(d => (d.SectionId, d.ParentId))
            .Distinct()
            .Where(scope => scope != (resolvedSectionId, targetParentId))
            .ToList();

        var globalDepartures = new HashSet<int>();
        var globalArrivals = new List<AbwabDoor>();

        foreach (var doorRef in doors)
        {
            var door = loaded.Single(d => d.Id == doorRef.DoorId);
            db.Entry(door).Property(d => d.Version).OriginalValue = doorRef.Version;

            var wasRoot = door.ParentId is null;
            door.SectionId = resolvedSectionId;
            door.ParentId = targetParentId;
            door.UpdatedAtUtc = now;

            if (wasRoot && targetParentId is not null)
            {
                door.GlobalOrderValue = null;
                globalDepartures.Add(door.Id);
            }
            else if (!wasRoot && targetParentId is null)
            {
                globalArrivals.Add(door);
            }
        }

        if (sectionChanged)
        {
            await CascadeSectionToDescendantsAsync(childrenByParent, movedIds, resolvedSectionId, now, cancellationToken);
        }

        foreach (var (sectionId, parentId) in oldScopes)
        {
            await ResequenceSiblingsExcludingAsync(sectionId, parentId, movedIds, cancellationToken);
        }

        Resequence(existingDestinationSiblings
            .Concat(doors.Select(doorRef => loaded.Single(d => d.Id == doorRef.DoorId))));

        if (globalDepartures.Count > 0 || globalArrivals.Count > 0)
        {
            await MaintainGlobalOrderAsync(globalDepartures, globalArrivals, cancellationToken);
        }

        await SaveTranslatingWriteExceptionsAsync(null, cancellationToken);

        return await ToDtosAsync(loaded, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> BulkArchiveAsync(IReadOnlyList<AbwabBulkDoorRef> doors, CancellationToken cancellationToken)
    {
        var ids = doors.Select(d => d.DoorId).ToList();
        var loaded = await db.AbwabDoors
            .Where(d => ids.Contains(d.Id) && d.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (loaded.Count != ids.Distinct().Count())
        {
            throw new AbwabNotFoundException();
        }

        foreach (var doorRef in doors)
        {
            var door = loaded.Single(d => d.Id == doorRef.DoorId);
            db.Entry(door).Property(d => d.Version).OriginalValue = doorRef.Version;
        }

        var now = DateTimeOffset.UtcNow;

        var archivedIds = new HashSet<int>();
        var oldScopes = loaded.Select(d => (d.SectionId, d.ParentId)).Distinct().ToList();
        var topLevelIds = loaded.Select(d => d.Id).ToHashSet();
        var globalDepartures = loaded.Where(d => d.ParentId is null).Select(d => d.Id).ToHashSet();
        var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);

        foreach (var door in loaded)
        {
            archivedIds.UnionWith(await ArchiveSubtreeAsync(door, childrenByParent, now, cancellationToken));
        }

        foreach (var (sectionId, parentId) in oldScopes)
        {
            await ResequenceSiblingsExcludingAsync(sectionId, parentId, topLevelIds, cancellationToken);
        }

        if (globalDepartures.Count > 0)
        {
            await MaintainGlobalOrderAsync(globalDepartures, [], cancellationToken);
        }

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return [.. archivedIds];
    }

    public async Task<bool> DeleteAsync(int id, uint expectedVersion, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAtUtc == null, cancellationToken);
        if (door is null)
        {
            return false;
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var now = DateTimeOffset.UtcNow;
        var oldSectionId = door.SectionId;
        var oldParentId = door.ParentId;
        var wasRoot = oldParentId is null;

        await ArchiveSubtreeAsync(door, await LoadChildrenByParentAsync(cancellationToken), now, cancellationToken);
        await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);

        if (wasRoot)
        {
            await MaintainGlobalOrderAsync(new HashSet<int> { id }, [], cancellationToken);
        }

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return true;
    }

    public async Task<AbwabDoorDto?> RestoreAsync(int id, int? sectionId, uint expectedVersion, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (door is null)
        {
            return null;
        }

        AbwabDoor? parent = null;
        if (door.ParentId.HasValue)
        {
            parent = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == door.ParentId.Value, cancellationToken);
            if (parent is { DeletedAtUtc: not null })
            {
                throw new AbwabParentStillArchivedException();
            }
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var archivedAt = door.DeletedAtUtc;

        var now = DateTimeOffset.UtcNow;
        door.DeletedAtUtc = null;
        door.UpdatedAtUtc = now;

        if (archivedAt.HasValue)
        {
            var previousSectionId = door.SectionId;
            door.SectionId = await ResolveRestoreSectionAsync(door, parent, sectionId, cancellationToken);

            var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);

            var descendantIds = CollectDescendantIds(childrenByParent, id);
            var sweptInDescendants = descendantIds.Count == 0
                ? []
                : await db.AbwabDoors
                    .Where(d => descendantIds.Contains(d.Id) && d.DeletedAtUtc == archivedAt.Value)
                    .ToListAsync(cancellationToken);

            foreach (var descendant in sweptInDescendants)
            {
                descendant.DeletedAtUtc = null;
                descendant.UpdatedAtUtc = now;
            }

            if (previousSectionId != door.SectionId)
            {
                await CascadeSectionToDescendantsAsync(childrenByParent, [id], door.SectionId, now, cancellationToken);
            }

            if (door.ParentId is null)
            {
                await MaintainGlobalOrderAsync(new HashSet<int>(), [door], cancellationToken);
            }

            var scopeSiblings = await db.AbwabDoors
                .Where(d => d.SectionId == door.SectionId && d.ParentId == door.ParentId
                            && d.DeletedAtUtc == null && d.Id != id)
                .OrderBy(d => d.OrderValue)
                .ToListAsync(cancellationToken);
            Resequence(scopeSiblings.Append(door));
        }

        await SaveTranslatingWriteExceptionsAsync(door.Name, cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    private async Task<int> ResolveRestoreSectionAsync(
        AbwabDoor door, AbwabDoor? parent, int? statedSectionId, CancellationToken cancellationToken)
    {
        if (parent is not null)
        {
            if (statedSectionId.HasValue && statedSectionId != parent.SectionId)
            {
                throw new AbwabSectionParentMismatchException();
            }

            return parent.SectionId;
        }

        if (statedSectionId.HasValue)
        {
            await EnsureSectionExistsAsync(statedSectionId.Value, cancellationToken);
            return statedSectionId.Value;
        }

        if (await IsSectionArchivedAsync(door.SectionId, cancellationToken))
        {
            throw new AbwabSectionRequiredException();
        }

        return door.SectionId;
    }

    private async Task<int> ResolveCreateSectionAsync(int? parentId, int? sectionId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            if (!sectionId.HasValue)
            {
                throw new AbwabSectionRequiredException();
            }

            await EnsureSectionExistsAsync(sectionId.Value, cancellationToken);
            return sectionId.Value;
        }

        var parent = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == parentId.Value && d.DeletedAtUtc == null, cancellationToken);
        if (parent is null)
        {
            throw new AbwabParentNotFoundException();
        }

        if (sectionId.HasValue)
        {
            await EnsureSectionExistsAsync(sectionId.Value, cancellationToken);
            if (sectionId != parent.SectionId)
            {
                throw new AbwabSectionParentMismatchException();
            }
        }

        return parent.SectionId;
    }

    private async Task EnsureSectionExistsAsync(int sectionId, CancellationToken cancellationToken)
    {
        if (!await db.AbwabSections.AnyAsync(s => s.Id == sectionId && s.DeletedAtUtc == null, cancellationToken))
        {
            throw new AbwabSectionNotFoundException();
        }
    }

    private async Task CascadeSectionToDescendantsAsync(
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        IEnumerable<int> movedIds,
        int sectionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var descendantIds = movedIds
            .SelectMany(movedId => CollectDescendantIds(childrenByParent, movedId))
            .Distinct()
            .ToList();
        if (descendantIds.Count == 0)
        {
            return;
        }

        var descendants = await db.AbwabDoors
            .Where(d => descendantIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        foreach (var descendant in descendants.Where(d => d.SectionId != sectionId))
        {
            descendant.SectionId = sectionId;
            descendant.UpdatedAtUtc = now;
        }
    }

    private async Task<bool> IsSectionArchivedAsync(int sectionId, CancellationToken cancellationToken) =>
        await db.AbwabSections.AnyAsync(s => s.Id == sectionId && s.DeletedAtUtc != null, cancellationToken);

    private async Task<int> ResolveTargetSectionAsync(int? targetSectionId, int? targetParentId, CancellationToken cancellationToken)
    {
        if (!targetParentId.HasValue)
        {
            if (!targetSectionId.HasValue)
            {
                throw new AbwabSectionRequiredException();
            }

            await EnsureSectionExistsAsync(targetSectionId.Value, cancellationToken);
            return targetSectionId.Value;
        }

        var parent = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == targetParentId.Value && d.DeletedAtUtc == null, cancellationToken);
        if (parent is null)
        {
            throw new AbwabParentNotFoundException();
        }

        return parent.SectionId;
    }

    private static void EnsureNotCycle(IReadOnlyDictionary<int, List<int>> childrenByParent, int doorId, int targetParentId)
    {
        if (targetParentId == doorId)
        {
            throw new AbwabCycleException();
        }

        if (CollectDescendantIds(childrenByParent, doorId).Contains(targetParentId))
        {
            throw new AbwabCycleException();
        }
    }

    private async Task<List<int>> ArchiveSubtreeAsync(
        AbwabDoor door,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var archived = new List<int> { door.Id };
        door.DeletedAtUtc = now;
        door.UpdatedAtUtc = now;

        if (door.ParentId is null)
        {
            door.GlobalOrderValue = null;
        }

        var descendantIds = CollectDescendantIds(childrenByParent, door.Id);
        if (descendantIds.Count == 0)
        {
            return archived;
        }

        var liveDescendants = await db.AbwabDoors
            .Where(d => descendantIds.Contains(d.Id) && d.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var descendant in liveDescendants)
        {
            descendant.DeletedAtUtc = now;
            descendant.UpdatedAtUtc = now;
            archived.Add(descendant.Id);
        }

        return archived;
    }

    private async Task<Dictionary<int, List<int>>> LoadChildrenByParentAsync(CancellationToken cancellationToken)
    {
        var all = await db.AbwabDoors
            .Select(d => new { d.Id, d.ParentId })
            .ToListAsync(cancellationToken);

        return all
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToList());
    }

    private static List<int> CollectDescendantIds(IReadOnlyDictionary<int, List<int>> childrenByParent, int doorId)
    {
        var result = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(doorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                result.Add(child);
                queue.Enqueue(child);
            }
        }

        return result;
    }

    private async Task ResequenceSiblingsExcludingAsync(
        int sectionId, int? parentId, IReadOnlySet<int> excludeIds, CancellationToken cancellationToken)
    {
        var siblings = await db.AbwabDoors
            .Where(d => d.SectionId == sectionId && d.ParentId == parentId && d.DeletedAtUtc == null)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        Resequence(siblings.Where(d => !excludeIds.Contains(d.Id)));
    }

    private static void Resequence(IEnumerable<AbwabDoor> orderedSiblings)
    {
        var position = 1;
        foreach (var door in orderedSiblings)
        {
            door.OrderValue = position++;
        }
    }

    private static void ResequenceGlobal(IEnumerable<AbwabDoor> orderedRoots)
    {
        var position = 1;
        foreach (var door in orderedRoots)
        {
            door.GlobalOrderValue = position++;
        }
    }

    private async Task MaintainGlobalOrderAsync(
        IReadOnlySet<int> excludeIds, IReadOnlyList<AbwabDoor> arrivals, CancellationToken cancellationToken)
    {
        var liveRoots = await db.AbwabDoors
            .Where(d => d.ParentId == null && d.DeletedAtUtc == null)
            .OrderBy(d => d.GlobalOrderValue).ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        ResequenceGlobal(liveRoots.Where(d => !excludeIds.Contains(d.Id)).Concat(arrivals));
    }

    private async Task ReplaceAliasesAsync(int doorId, IReadOnlyList<string> newAliases, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalizedNew = AbwabAliasNormalization.Normalize(newAliases);

        var existingLive = await db.AbwabDoorAliases
            .Where(a => a.DoorId == doorId && a.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);
        var existingValues = existingLive.Select(a => a.Value).ToHashSet();

        foreach (var alias in existingLive.Where(a => !normalizedNew.Contains(a.Value)))
        {
            alias.DeletedAtUtc = now;
            alias.UpdatedAtUtc = now;
        }

        foreach (var value in normalizedNew.Where(v => !existingValues.Contains(v)))
        {
            db.AbwabDoorAliases.Add(new AbwabDoorAlias
            {
                DoorId = doorId,
                Value = value,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
    }

    private async Task<AbwabDoorDto> ToDtoAsync(AbwabDoor door, CancellationToken cancellationToken)
    {
        var aliases = await db.AbwabDoorAliases
            .Where(a => a.DoorId == door.Id && a.DeletedAtUtc == null)
            .Select(a => a.Value)
            .ToListAsync(cancellationToken);

        return new AbwabDoorDto(
            door.Id, door.SectionId, door.ParentId, door.Name, door.Description,
            door.RepresentativeAyahText, door.OrderValue, door.GlobalOrderValue, door.Version, aliases);
    }

    private async Task<IReadOnlyList<AbwabDoorDto>> ToDtosAsync(IEnumerable<AbwabDoor> doors, CancellationToken cancellationToken)
    {
        var result = new List<AbwabDoorDto>();
        foreach (var door in doors)
        {
            result.Add(await ToDtoAsync(door, cancellationToken));
        }

        return result;
    }

    private async Task SaveTranslatingWriteExceptionsAsync(string? name, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AbwabStaleVersionException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new AbwabDuplicateNameException();
        }
    }

    private async Task SaveTranslatingConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AbwabStaleVersionException();
        }
    }
}
