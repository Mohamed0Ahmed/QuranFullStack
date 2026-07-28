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
        if (targetParentId.HasValue)
        {
            EnsureNotCycle(await LoadChildrenByParentAsync(cancellationToken), id, targetParentId.Value);
        }

        var oldSectionId = door.SectionId;
        var oldParentId = door.ParentId;
        var scopeIsUnchanged = oldSectionId == resolvedSectionId && oldParentId == targetParentId;

        db.Entry(door).Property(d => d.Version).OriginalValue = expectedVersion;

        // Queried BEFORE the door is mutated and with the door itself excluded. The database row still
        // shows its OLD scope until SaveChanges, so a move whose destination IS the door's current scope
        // would otherwise count the door twice and leave {1..N-1, N+1} — the single-door twin of the
        // overlap case BulkMoveAsync handles below.
        var destinationSiblings = await db.AbwabDoors
            .Where(d => d.SectionId == resolvedSectionId && d.ParentId == targetParentId
                        && d.DeletedAtUtc == null && d.Id != id)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);

        door.SectionId = resolvedSectionId;
        door.ParentId = targetParentId;
        door.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (!scopeIsUnchanged)
        {
            await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);
        }

        Resequence(destinationSiblings.Append(door));

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
        Resequence(siblings);

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
            var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);
            foreach (var door in loaded)
            {
                EnsureNotCycle(childrenByParent, door.Id, targetParentId.Value);
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
        Resequence(existingDestinationSiblings
            .Concat(doors.Select(doorRef => loaded.Single(d => d.Id == doorRef.DoorId))));

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

        // A set, not a list: selecting a door AND one of its own descendants in the same batch is legal
        // (the UI selects rows, not subtrees), and the descendant is then reached twice — once swept in by
        // its ancestor, once as a top-level entry. It was archived once; it is reported once.
        var archivedIds = new HashSet<int>();
        var oldScopes = loaded.Select(d => (d.SectionId, d.ParentId)).Distinct().ToList();
        var topLevelIds = loaded.Select(d => d.Id).ToHashSet();
        var childrenByParent = await LoadChildrenByParentAsync(cancellationToken);

        foreach (var door in loaded)
        {
            archivedIds.UnionWith(await ArchiveSubtreeAsync(door, childrenByParent, now, cancellationToken));
        }

        foreach (var (sectionId, parentId) in oldScopes)
        {
            await ResequenceSiblingsExcludingAsync(sectionId, parentId, topLevelIds, cancellationToken);
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

        await ArchiveSubtreeAsync(door, await LoadChildrenByParentAsync(cancellationToken), now, cancellationToken);
        await ResequenceSiblingsExcludingAsync(oldSectionId, oldParentId, new HashSet<int> { id }, cancellationToken);

        await SaveTranslatingConcurrencyAsync(cancellationToken);

        return true;
    }

    public async Task<AbwabRestoredDoorDto?> RestoreAsync(int id, uint expectedVersion, CancellationToken cancellationToken)
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

        // Captured BEFORE DeletedAtUtc is cleared: it is the only discriminator between the rows this
        // door's own archive swept in and rows an earlier, separate archive claimed. Archive only ever
        // takes LIVE descendants, so restore may only give back what that same archive took — resurrecting
        // a descendant the user archived deliberately days earlier is not symmetry, it is data coming back
        // unasked.
        var archivedAt = door.DeletedAtUtc;

        var now = DateTimeOffset.UtcNow;
        door.DeletedAtUtc = null;
        door.UpdatedAtUtc = now;

        // A section can only be archived once it holds no LIVE doors, so an archived section here means it
        // was retired while this door sat archived. Sections have no restore route in this slice, so a 409
        // would strand the door permanently; it is detached to "outside every section" instead — a
        // first-class state (plan §R8, what makes «كل الأبواب» a real superset), not an invented home.
        var sectionWasArchived = await IsSectionArchivedAsync(door.SectionId, cancellationToken);
        if (sectionWasArchived)
        {
            door.SectionId = null;
        }

        if (archivedAt.HasValue)
        {
            var descendantIds = CollectDescendantIds(await LoadChildrenByParentAsync(cancellationToken), id);
            var sweptInDescendants = descendantIds.Count == 0
                ? []
                : await db.AbwabDoors
                    .Where(d => descendantIds.Contains(d.Id) && d.DeletedAtUtc == archivedAt.Value)
                    .ToListAsync(cancellationToken);

            foreach (var descendant in sweptInDescendants)
            {
                descendant.DeletedAtUtc = null;
                descendant.UpdatedAtUtc = now;

                // A nested door always inherits its parent's section (ResolveTargetSectionAsync), so a
                // detached subtree has to be detached whole or that invariant breaks on the read side.
                if (sectionWasArchived)
                {
                    descendant.SectionId = null;
                }
            }
        }

        // Restore is the only write that moves a row back INTO a scope. That scope was renumbered 1..N-1
        // when the door left it, so it needs renumbering again with the door back in — the same 1..N rule
        // every other write follows. Read against the door's FINAL scope, which the detach above may have
        // just changed.
        var scopeSiblings = await db.AbwabDoors
            .Where(d => d.SectionId == door.SectionId && d.ParentId == door.ParentId
                        && d.DeletedAtUtc == null && d.Id != id)
            .OrderBy(d => d.OrderValue)
            .ToListAsync(cancellationToken);
        Resequence(scopeSiblings.Append(door));

        // Not SaveTranslatingConcurrencyAsync: restore moves rows back INTO the unique index's live
        // scope (unlike archive/reorder, which only ever move rows out of it), so a live sibling
        // already named the same as the restored door — or one of its restored descendants — is a
        // real 23505 risk here, not a structurally-impossible one.
        await SaveTranslatingWriteExceptionsAsync(door.Name, cancellationToken);

        return new AbwabRestoredDoorDto(await ToDtoAsync(door, cancellationToken), sectionWasArchived);
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

    private async Task<bool> IsSectionArchivedAsync(int? sectionId, CancellationToken cancellationToken) =>
        sectionId.HasValue
        && await db.AbwabSections.AnyAsync(s => s.Id == sectionId.Value && s.DeletedAtUtc != null, cancellationToken);

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

    // Archives door + its whole live subtree (unlimited depth — plan §4 sets no depth limit). Returns
    // every id archived by this call, including swept-in descendants, for BulkArchive's response.
    private async Task<List<int>> ArchiveSubtreeAsync(
        AbwabDoor door,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var archived = new List<int> { door.Id };
        door.DeletedAtUtc = now;
        door.UpdatedAtUtc = now;

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

    // One projection of every (id, parent_id) pair, built ONCE per operation and shared by every descendant
    // walk inside it. Loading it per call turned a 50-door bulk move into 50 full reads of abwab_doors.
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

    // BFS over the parent map regardless of archived state — a cycle guard must see archived descendants
    // too, since their parent_id survives soft-delete.
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

    // Shared by every write that touches Name's uniqueness scope (create/edit/move/bulk-move/restore).
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

    // Shared by writes that only ever move a row out of the unique index's live scope (archive, reorder)
    // — a duplicate-name violation is structurally impossible for these.
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
