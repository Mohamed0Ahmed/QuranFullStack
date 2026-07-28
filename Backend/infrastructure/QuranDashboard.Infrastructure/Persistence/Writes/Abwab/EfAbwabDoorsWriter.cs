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
        await EnsureParentAndSectionExistAsync(parentId, sectionId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var nextOrder = await db.AbwabDoors.CountAsync(
            d => d.SectionId == sectionId && d.ParentId == parentId && d.DeletedAtUtc == null, cancellationToken) + 1;

        var door = new AbwabDoor
        {
            SectionId = sectionId,
            ParentId = parentId,
            Name = name,
            Description = description,
            RepresentativeAyahText = representativeAyahText,
            OrderValue = nextOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AbwabDoors.Add(door);

        // Two SaveChanges calls (door, then aliases keyed by its generated id) need an explicit
        // transaction to stay atomic — EF's implicit per-SaveChanges transaction only covers one call.
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
        await EnsureNotCycleAsync(id, targetParentId, cancellationToken);

        var oldSectionId = door.SectionId;
        var oldParentId = door.ParentId;

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var now = DateTimeOffset.UtcNow;
        var newSiblingCount = await db.AbwabDoors.CountAsync(
            d => d.SectionId == resolvedSectionId && d.ParentId == targetParentId && d.DeletedAtUtc == null, cancellationToken);

        door.SectionId = resolvedSectionId;
        door.ParentId = targetParentId;
        door.OrderValue = newSiblingCount + 1;
        door.UpdatedAtUtc = now;

        await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);

        await SaveTranslatingWriteExceptionsAsync(door.Name, cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    public async Task<AbwabDoorDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAtUtc == null, cancellationToken);
        if (door is null)
        {
            return null;
        }

        var siblings = await db.AbwabDoors
            .Where(d => d.SectionId == door.SectionId && d.ParentId == door.ParentId && d.DeletedAtUtc == null)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        if (position < 1 || position > siblings.Count)
        {
            throw new AbwabInvalidPositionException();
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        siblings.RemoveAll(s => s.Id == id);
        siblings.Insert(position - 1, door);

        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].OrderValue = i + 1;
        }

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
        if (doors.Count == 0)
        {
            return [];
        }

        var ids = doors.Select(d => d.DoorId).ToList();
        var loaded = await db.AbwabDoors
            .Where(d => ids.Contains(d.Id) && d.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (loaded.Count != ids.Distinct().Count())
        {
            throw new AbwabNotFoundException();
        }

        var resolvedSectionId = await ResolveTargetSectionAsync(targetSectionId, targetParentId, cancellationToken);
        if (targetParentId.HasValue)
        {
            foreach (var door in loaded)
            {
                await EnsureNotCycleAsync(door.Id, targetParentId, cancellationToken);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var movedIds = loaded.Select(d => d.Id).ToHashSet();

        // Queried BEFORE any door is mutated, and excluding movedIds defensively: a moved door whose
        // OLD scope already equals the destination would otherwise be double-counted by a plain
        // COUNT/append (the DB row still shows its old FK values until SaveChanges, so a naive
        // "existing count + 1" is stale the moment a moved door's source scope is the destination).
        var existingDestinationSiblings = await db.AbwabDoors
            .Where(d => d.SectionId == resolvedSectionId && d.ParentId == targetParentId
                        && d.DeletedAtUtc == null && !movedIds.Contains(d.Id))
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        var oldScopes = loaded.Select(d => (d.SectionId, d.ParentId))
            .Distinct()
            .Where(scope => scope != (resolvedSectionId, targetParentId))
            .ToList();

        foreach (var doorRef in doors)
        {
            var door = loaded.Single(d => d.Id == doorRef.DoorId);
            db.Entry(door).Property(d => d.Version).OriginalValue = doorRef.Version;

            door.SectionId = resolvedSectionId;
            door.ParentId = targetParentId;
            door.UpdatedAtUtc = now;
        }

        foreach (var (sectionId, parentId) in oldScopes)
        {
            await ResequenceSiblingsExcludingAsync(sectionId, parentId, movedIds, cancellationToken);
        }

        // The moved doors are appended after the destination's existing (untouched) siblings, in the
        // batch's own order, then the whole destination scope is renumbered 1..N together — the only
        // way to keep it contiguous when a moved door's old scope is also the destination.
        var destinationFinalOrder = existingDestinationSiblings
            .Concat(doors.Select(doorRef => loaded.Single(d => d.Id == doorRef.DoorId)))
            .ToList();
        for (var i = 0; i < destinationFinalOrder.Count; i++)
        {
            destinationFinalOrder[i].OrderValue = i + 1;
        }

        await SaveTranslatingWriteExceptionsAsync(null, cancellationToken);

        return await ToDtosAsync(loaded, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> BulkArchiveAsync(IReadOnlyList<AbwabBulkDoorRef> doors, CancellationToken cancellationToken)
    {
        if (doors.Count == 0)
        {
            return [];
        }

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
        var archivedIds = new List<int>();
        var oldScopes = loaded.Select(d => (d.SectionId, d.ParentId)).Distinct().ToList();
        var topLevelIds = loaded.Select(d => d.Id).ToHashSet();

        foreach (var door in loaded)
        {
            archivedIds.AddRange(await ArchiveSubtreeAsync(door, now, cancellationToken));
        }

        foreach (var (sectionId, parentId) in oldScopes)
        {
            await ResequenceSiblingsExcludingAsync(sectionId, parentId, topLevelIds, cancellationToken);
        }

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return archivedIds;
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

        await ArchiveSubtreeAsync(door, now, cancellationToken);
        await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return true;
    }

    public async Task<AbwabDoorDto?> RestoreAsync(int id, uint expectedVersion, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (door is null)
        {
            return null;
        }

        if (door.ParentId.HasValue)
        {
            var parent = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == door.ParentId.Value, cancellationToken);
            if (parent is { DeletedAtUtc: not null })
            {
                throw new AbwabParentStillArchivedException();
            }
        }

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        var now = DateTimeOffset.UtcNow;
        door.DeletedAtUtc = null;
        door.UpdatedAtUtc = now;

        var descendantIds = await GetDescendantIdsAsync(id, cancellationToken);
        if (descendantIds.Count > 0)
        {
            var archivedDescendants = await db.AbwabDoors
                .Where(d => descendantIds.Contains(d.Id) && d.DeletedAtUtc != null)
                .ToListAsync(cancellationToken);

            foreach (var descendant in archivedDescendants)
            {
                descendant.DeletedAtUtc = null;
                descendant.UpdatedAtUtc = now;
            }
        }

        // Not SaveTranslatingConcurrencyAsync: restore moves rows back INTO the unique index's live
        // scope (unlike archive/reorder, which only ever move rows out of it), so a live sibling
        // already named the same as the restored door — or one of its restored descendants — is a
        // real 23505 risk here, not a structurally-impossible one.
        await SaveTranslatingWriteExceptionsAsync(door.Name, cancellationToken);

        return await ToDtoAsync(door, cancellationToken);
    }

    private async Task EnsureParentAndSectionExistAsync(int? parentId, int? sectionId, CancellationToken cancellationToken)
    {
        if (parentId.HasValue)
        {
            var parentExists = await db.AbwabDoors.AnyAsync(d => d.Id == parentId.Value && d.DeletedAtUtc == null, cancellationToken);
            if (!parentExists)
            {
                throw new AbwabParentNotFoundException();
            }
        }

        if (sectionId.HasValue)
        {
            var sectionExists = await db.AbwabSections.AnyAsync(s => s.Id == sectionId.Value && s.DeletedAtUtc == null, cancellationToken);
            if (!sectionExists)
            {
                throw new AbwabSectionNotFoundException();
            }
        }
    }

    // targetSectionId is only honored when targetParentId is null — nesting under a parent always
    // inherits that parent's own section, so the two inputs can never disagree.
    private async Task<int?> ResolveTargetSectionAsync(int? targetSectionId, int? targetParentId, CancellationToken cancellationToken)
    {
        if (!targetParentId.HasValue)
        {
            if (targetSectionId.HasValue)
            {
                var sectionExists = await db.AbwabSections.AnyAsync(s => s.Id == targetSectionId.Value && s.DeletedAtUtc == null, cancellationToken);
                if (!sectionExists)
                {
                    throw new AbwabSectionNotFoundException();
                }
            }

            return targetSectionId;
        }

        var parent = await db.AbwabDoors.FirstOrDefaultAsync(d => d.Id == targetParentId.Value && d.DeletedAtUtc == null, cancellationToken);
        if (parent is null)
        {
            throw new AbwabParentNotFoundException();
        }

        return parent.SectionId;
    }

    private async Task EnsureNotCycleAsync(int doorId, int? targetParentId, CancellationToken cancellationToken)
    {
        if (!targetParentId.HasValue)
        {
            return;
        }

        if (targetParentId.Value == doorId)
        {
            throw new AbwabCycleException();
        }

        var descendantIds = await GetDescendantIdsAsync(doorId, cancellationToken);
        if (descendantIds.Contains(targetParentId.Value))
        {
            throw new AbwabCycleException();
        }
    }

    // Archives door + its whole live subtree (unlimited depth — plan §4 sets no depth limit). Returns
    // every id archived by this call, including swept-in descendants, for BulkArchive's response.
    private async Task<List<int>> ArchiveSubtreeAsync(AbwabDoor door, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var archived = new List<int> { door.Id };
        door.DeletedAtUtc = now;
        door.UpdatedAtUtc = now;

        var descendantIds = await GetDescendantIdsAsync(door.Id, cancellationToken);
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

    // BFS over every door's parent pointer, regardless of archived state — a cycle guard must see
    // archived descendants too, since their parent_id survives soft-delete.
    private async Task<List<int>> GetDescendantIdsAsync(int doorId, CancellationToken cancellationToken)
    {
        var all = await db.AbwabDoors
            .Select(d => new { d.Id, d.ParentId })
            .ToListAsync(cancellationToken);

        var childrenByParent = all
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToList());

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

    // excludeIds always contains the door(s) just moved/archived out of this scope: querying live rows
    // by (sectionId, parentId) hits the database directly, which still shows their OLD, pre-save FK
    // values, so without this exclusion they'd be resequenced as if they were still here.
    private async Task ResequenceSiblingsExcludingAsync(
        int? sectionId, int? parentId, IReadOnlySet<int> excludeIds, CancellationToken cancellationToken)
    {
        var siblings = await db.AbwabDoors
            .Where(d => d.SectionId == sectionId && d.ParentId == parentId && d.DeletedAtUtc == null)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        var remaining = siblings.Where(d => !excludeIds.Contains(d.Id)).ToList();
        for (var i = 0; i < remaining.Count; i++)
        {
            remaining[i].OrderValue = i + 1;
        }
    }

    // Reused by both Create (existingLive is empty, so every alias is an insert) and Edit (a full diff).
    private async Task ReplaceAliasesAsync(int doorId, IReadOnlyList<string> newAliases, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalizedNew = newAliases
            .Select(a => a.Trim())
            .Where(a => a.Length > 0)
            .Distinct()
            .ToList();

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
            door.RepresentativeAyahText, door.OrderValue, door.Version, aliases);
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

    // Shared by every write that touches Name's uniqueness scope (create/edit/move/bulk-move).
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
            throw new AbwabDuplicateNameException(name);
        }
    }

    // Shared by writes that only ever move a row out of the unique index's live scope (archive,
    // restore, reorder) — a duplicate-name violation is structurally impossible for these.
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
