using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal sealed class EfAbwabTemplateApplyWriter(QuranDashboardDbContext db) : IAbwabTemplateApplyWriter
{
    private sealed record CopiedNode(AbwabDoor Door, AbwabTemplateNode Node, int SectionId);

    public async Task<IReadOnlyList<AbwabDoorDto>> ApplyAsync(
        int templateId,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken)
    {
        var targetIds = targetDoorIds.Distinct().ToList();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

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

        targets = [.. targets.OrderBy(t => targetIds.IndexOf(t.Id))];

        if (targets.Exists(t => t.DeletedAtUtc != null))
        {
            throw new AbwabTemplateTargetArchivedException();
        }

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
            var nextOrder = await db.AbwabDoors.CountAsync(
                d => d.ParentId == target.Id && d.DeletedAtUtc == null, cancellationToken) + 1;

            for (var i = 0; i < rootChildren.Count; i++)
            {
                var child = rootChildren[i];
                var copiedChild = NewDoor(child, target.SectionId, target.Id, nextOrder + i, now);
                db.AbwabDoors.Add(copiedChild);
                createdRoots.Add(new CopiedNode(copiedChild, child, target.SectionId));
            }
        }

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
                    var copiedChild = NewDoor(child, copied.SectionId, copied.Door.Id, child.OrderValue, now);
                    db.AbwabDoors.Add(copiedChild);
                    nextLevel.Add(new CopiedNode(copiedChild, child, copied.SectionId));
                }
            }

            await SaveTranslatingDuplicateNameAsync(cancellationToken);
            level = nextLevel;
        }

        await transaction.CommitAsync(cancellationToken);

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
        AbwabTemplateNode node, int sectionId, int parentId, int orderValue, DateTimeOffset now) =>
        new()
        {
            SectionId = sectionId,
            ParentId = parentId,
            Name = node.Name,
            Description = node.Description,
            RepresentativeAyahText = node.RepresentativeAyahText,
            OrderValue = orderValue,
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
