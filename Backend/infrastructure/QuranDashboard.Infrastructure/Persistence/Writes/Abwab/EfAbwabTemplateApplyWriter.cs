using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

/// <remarks>
/// What this writer deliberately does NOT do, and why — each is a door-write mechanism with no work
/// here: no global-order maintenance (a copy is never a root, so it never joins that sequence); no
/// resequencing (every insert appends into a scope it either just created or is the newest member
/// of — the level-1 offset (<c>nextOrder + i</c>) is what keeps this true when N children land in
/// one save — so all touched scopes stay 1..N by construction); no per-node section resolution (the
/// section is read once off each target and carried down the whole subtree, which is the cascade
/// invariant stated directly).
///
/// Copies the template root's DIRECT CHILDREN as new children of each target, recursively copying
/// each of their subtrees. The root itself is never copied (ux-slice-g reversal of the original
/// "root becomes a new child" axiom — docs/feature-abwab-templates/plan.md §5.1).
/// </remarks>
internal sealed class EfAbwabTemplateApplyWriter(QuranDashboardDbContext db) : IAbwabTemplateApplyWriter
{
    private sealed record CopiedNode(AbwabDoor Door, AbwabTemplateNode Node, int? SectionId);

    public async Task<IReadOnlyList<AbwabDoorDto>> ApplyAsync(
        int templateId,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken)
    {
        var targetIds = targetDoorIds.Distinct().ToList();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Read once, inside the transaction. A concurrent template edit either commits before this
        // read (copied) or after it (not copied); both are legitimate outcomes, and no token is
        // offered because the caller holds no template version.
        var templateExists = await db.AbwabTemplates.AsNoTracking()
            .AnyAsync(t => t.Id == templateId && t.DeletedAtUtc == null, cancellationToken);
        if (!templateExists)
        {
            throw new AbwabTemplateNotFoundException();
        }

        var nodes = await db.AbwabTemplateNodes.AsNoTracking()
            .Where(n => n.TemplateId == templateId && n.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);

        var rootNode = nodes.Find(n => n.ParentNodeId is null)
            ?? throw new AbwabTemplateNotFoundException();

        var childrenByParentNode = nodes
            .Where(n => n.ParentNodeId is not null)
            .GroupBy(n => n.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(n => n.OrderValue).ThenBy(n => n.Id).ToList());

        // The template's emptiness is a property of the template alone — it does not depend on which
        // doors were picked, so this refusal fires before the target reads. Consequence: an empty
        // template applied to an archived target returns THIS 400, not the archived-target 400.
        if (!childrenByParentNode.TryGetValue(rootNode.Id, out var rootChildren) || rootChildren.Count == 0)
        {
            throw new AbwabTemplateEmptyException();
        }

        var targets = await db.AbwabDoors.AsNoTracking()
            .Where(d => targetIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name, d.SectionId, d.DeletedAtUtc })
            .ToListAsync(cancellationToken);

        if (targets.Count != targetIds.Count)
        {
            throw new AbwabNotFoundException();
        }

        // The response lists N created children per target (the root's direct children), and the
        // collision message names (target, child) pairs in the same order — the CALLER's target
        // order, then the template's own sibling order for the names under each target.
        targets = [.. targets.OrderBy(t => targetIds.IndexOf(t.Id))];

        if (targets.Exists(t => t.DeletedAtUtc != null))
        {
            throw new AbwabTemplateTargetArchivedException();
        }

        // Named up front so the 409 can say WHICH child name collided under WHICH target; 23505
        // names no row. The catch in the save helper stays as the race backstop, without names.
        var rootChildNames = rootChildren.Select(c => c.Name).ToHashSet();
        var collisionHits = await db.AbwabDoors.AsNoTracking()
            .Where(d => d.ParentId != null
                && targetIds.Contains(d.ParentId!.Value)
                && rootChildNames.Contains(d.Name)
                && d.DeletedAtUtc == null)
            .Select(d => new { ParentId = d.ParentId!.Value, d.Name })
            .ToListAsync(cancellationToken);

        if (collisionHits.Count > 0)
        {
            var siblingOrderByName = rootChildren
                .Select((child, index) => (child.Name, index))
                .ToDictionary(x => x.Name, x => x.index);

            var pairs = targets
                .SelectMany(target => collisionHits
                    .Where(hit => hit.ParentId == target.Id)
                    .OrderBy(hit => siblingOrderByName[hit.Name])
                    .Select(hit => new AbwabTemplateApplyCollisionPair(target.Name, hit.Name)))
                .ToList();

            throw new AbwabTemplateApplyCollisionException(pairs);
        }

        var now = DateTimeOffset.UtcNow;
        var createdRoots = new List<CopiedNode>(targets.Count * rootChildren.Count);

        foreach (var target in targets)
        {
            // Scoped by parent alone, where CreateAsync scopes by (section, parent). Equivalent here
            // and not a shortcut: every live child of a door carries that door's section by the
            // cascade invariant, so the section term could only ever narrow the count to itself.
            var nextOrder = await db.AbwabDoors.CountAsync(
                d => d.ParentId == target.Id && d.DeletedAtUtc == null, cancellationToken) + 1;

            // The root's direct children seed the BFS descent below, offset by their index so all N
            // land contiguously at nextOrder..nextOrder+N-1 — level 1's exception to the verbatim-
            // OrderValue rule the descent loop applies at every deeper level.
            for (var i = 0; i < rootChildren.Count; i++)
            {
                var child = rootChildren[i];
                var copiedChild = NewDoor(child, target.SectionId, target.Id, nextOrder + i, now);
                db.AbwabDoors.Add(copiedChild);
                createdRoots.Add(new CopiedNode(copiedChild, child, target.SectionId));
            }
        }

        // AbwabDoor carries no parent navigation property, so a child's ParentId can only be set
        // once its parent's id exists — the copy therefore descends one level per save. All of it
        // is inside the one transaction above, so the batch stays all-or-nothing.
        await SaveTranslatingDuplicateNameAsync(cancellationToken);

        var level = createdRoots;
        while (level.Count > 0)
        {
            var nextLevel = new List<CopiedNode>();
            foreach (var copied in level)
            {
                AddAliases(copied.Door.Id, copied.Node.Aliases, now);

                if (!childrenByParentNode.TryGetValue(copied.Node.Id, out var children))
                {
                    continue;
                }

                foreach (var child in children)
                {
                    // The node's own OrderValue is carried through verbatim — that is what preserves
                    // sibling order in every copied scope, contiguous because the template's own
                    // scopes are 1..N.
                    var copiedChild = NewDoor(child, copied.SectionId, copied.Door.Id, child.OrderValue, now);
                    db.AbwabDoors.Add(copiedChild);
                    nextLevel.Add(new CopiedNode(copiedChild, child, copied.SectionId));
                }
            }

            // Flushes this level's aliases together with the next level's doors; the final pass
            // flushes the deepest level's aliases with nothing new to add.
            await SaveTranslatingDuplicateNameAsync(cancellationToken);
            level = nextLevel;
        }

        await transaction.CommitAsync(cancellationToken);

        // Each door carries its OWN node's aliases — not the root's (ux-slice-g DRIFT-1). The alias
        // ROWS were already correct at any seed (AddAliases above is per-node); this only fixes what
        // the response payload reports.
        return createdRoots
            .Select(copied => new AbwabDoorDto(
                copied.Door.Id,
                copied.Door.SectionId,
                copied.Door.ParentId,
                copied.Door.Name,
                copied.Door.Description,
                copied.Door.RepresentativeAyahText,
                copied.Door.OrderValue,
                copied.Door.GlobalOrderValue,
                copied.Door.Version,
                AbwabAliasNormalization.Normalize(copied.Node.Aliases)))
            .ToList();
    }

    private static AbwabDoor NewDoor(
        AbwabTemplateNode node, int? sectionId, int parentId, int orderValue, DateTimeOffset now) =>
        new()
        {
            // Every copied node at every depth inherits the TARGET's section — the invariant every
            // door write owes the read side, stated once here instead of derived per node.
            SectionId = sectionId,
            ParentId = parentId,
            Name = node.Name,
            Description = node.Description,
            RepresentativeAyahText = node.RepresentativeAyahText,
            OrderValue = orderValue,
            // GlobalOrderValue stays null: a copy is never a root door.
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

    private void AddAliases(int doorId, IReadOnlyList<string> aliases, DateTimeOffset now)
    {
        foreach (var value in AbwabAliasNormalization.Normalize(aliases))
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

    // The duplicate here genuinely IS a door name — the inverse of the relations writer's case,
    // where the door-name-keyed message would have been the wrong constraint entirely. Only
    // reachable as a race: the pre-check already refused every collision it could see, and the
    // template's own sibling-name index makes an internal collision unrepresentable.
    private async Task SaveTranslatingDuplicateNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new AbwabTemplateApplyCollisionException([]);
        }
    }
}
