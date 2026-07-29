using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal sealed class EfAbwabTemplatesWriter(QuranDashboardDbContext db) : IAbwabTemplatesWriter
{
    public async Task<AbwabTemplateDto> CreateAsync(
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var template = new AbwabTemplate { CreatedAtUtc = now, UpdatedAtUtc = now };
        db.AbwabTemplates.Add(template);

        // Two SaveChanges calls (the template, then its root node keyed by the generated id) need an
        // explicit transaction — EF's implicit one covers a single call. The doors CreateAsync trap.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var root = new AbwabTemplateNode
        {
            TemplateId = template.Id,
            ParentNodeId = null,
            Name = name,
            Description = description,
            RepresentativeAyahText = representativeAyahText,
            Aliases = [.. aliases],
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AbwabTemplateNodes.Add(root);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AbwabTemplateDto(template.Id, root.Name, [ToDto(root)]);
    }

    public async Task<bool> DeleteAsync(int templateId, CancellationToken cancellationToken)
    {
        var template = await db.AbwabTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.DeletedAtUtc == null, cancellationToken);
        if (template is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        template.DeletedAtUtc = now;
        template.UpdatedAtUtc = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent delete won. The template is gone either way, which is what the caller
            // asked for — the EfAbwabRelationsWriter.DeleteAsync judgment.
            return false;
        }

        // The node rows are deliberately untouched: both reads filter by the template's own
        // deleted_at, so cascading would write rows nothing looks at.
        return true;
    }

    public async Task<AbwabTemplateNodeDto> AddNodeAsync(
        int templateId,
        int parentNodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        var templateExists = await db.AbwabTemplates.AsNoTracking()
            .AnyAsync(t => t.Id == templateId && t.DeletedAtUtc == null, cancellationToken);
        if (!templateExists)
        {
            throw new AbwabTemplateNotFoundException();
        }

        var parentExists = await db.AbwabTemplateNodes.AsNoTracking()
            .AnyAsync(
                n => n.Id == parentNodeId && n.TemplateId == templateId && n.DeletedAtUtc == null,
                cancellationToken);
        if (!parentExists)
        {
            throw new AbwabTemplateNodeNotFoundException();
        }

        // Append, never insert — the doors CreateAsync precedent. Every sibling scope stays 1..N by
        // construction, so nothing here resequences.
        var nextOrder = await db.AbwabTemplateNodes.CountAsync(
            n => n.TemplateId == templateId && n.ParentNodeId == parentNodeId && n.DeletedAtUtc == null,
            cancellationToken) + 1;

        var now = DateTimeOffset.UtcNow;
        var node = new AbwabTemplateNode
        {
            TemplateId = templateId,
            ParentNodeId = parentNodeId,
            Name = name,
            Description = description,
            RepresentativeAyahText = representativeAyahText,
            Aliases = [.. aliases],
            OrderValue = nextOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AbwabTemplateNodes.Add(node);
        await SaveTranslatingDuplicateNameAsync(cancellationToken);

        return ToDto(node);
    }

    public async Task<AbwabTemplateNodeDto?> EditNodeAsync(
        int nodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        var node = await db.AbwabTemplateNodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.DeletedAtUtc == null, cancellationToken);
        if (node is null)
        {
            return null;
        }

        node.Name = name;
        node.Description = description;
        node.RepresentativeAyahText = representativeAyahText;
        // A NEW array, never an in-place mutation: EF change-tracks this property by reference.
        node.Aliases = [.. aliases];
        node.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await SaveTranslatingDuplicateNameAsync(cancellationToken);

        return ToDto(node);
    }

    public async Task<AbwabTemplateNodeDto?> ReorderNodeAsync(
        int nodeId, int position, CancellationToken cancellationToken)
    {
        var node = await db.AbwabTemplateNodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.DeletedAtUtc == null, cancellationToken);
        if (node is null)
        {
            return null;
        }

        if (node.ParentNodeId is null)
        {
            throw new AbwabTemplateRootNodeException();
        }

        var siblings = await db.AbwabTemplateNodes
            .Where(n => n.TemplateId == node.TemplateId
                && n.ParentNodeId == node.ParentNodeId
                && n.DeletedAtUtc == null)
            .OrderBy(n => n.OrderValue)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);

        if (position < 1 || position > siblings.Count)
        {
            throw new AbwabInvalidPositionException();
        }

        siblings.Remove(node);
        siblings.Insert(position - 1, node);
        Resequence(siblings, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(node);
    }

    public async Task<AbwabTemplateNodeDeleteResult> DeleteNodeAsync(
        int nodeId, CancellationToken cancellationToken)
    {
        var node = await db.AbwabTemplateNodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.DeletedAtUtc == null, cancellationToken);
        if (node is null)
        {
            return AbwabTemplateNodeDeleteResult.NotFound;
        }

        if (node.ParentNodeId is null)
        {
            return AbwabTemplateNodeDeleteResult.IsRoot;
        }

        var liveNodes = await db.AbwabTemplateNodes
            .Where(n => n.TemplateId == node.TemplateId && n.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);

        // The subtree goes with it: a template child has no meaning without its parent. One parent
        // map for the whole operation, walked as a plain BFS.
        var doomedIds = CollectSubtreeIds(liveNodes, node.Id);
        var now = DateTimeOffset.UtcNow;
        foreach (var doomed in liveNodes.Where(n => doomedIds.Contains(n.Id)))
        {
            doomed.DeletedAtUtc = now;
            doomed.UpdatedAtUtc = now;
        }

        var remainingSiblings = liveNodes
            .Where(n => n.ParentNodeId == node.ParentNodeId && !doomedIds.Contains(n.Id))
            .OrderBy(n => n.OrderValue)
            .ThenBy(n => n.Id)
            .ToList();
        Resequence(remainingSiblings, now);

        await db.SaveChangesAsync(cancellationToken);

        return AbwabTemplateNodeDeleteResult.Deleted;
    }

    private static HashSet<int> CollectSubtreeIds(IReadOnlyList<AbwabTemplateNode> liveNodes, int rootId)
    {
        var childrenByParent = liveNodes
            .Where(n => n.ParentNodeId is not null)
            .GroupBy(n => n.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(n => n.Id).ToList());

        var collected = new HashSet<int> { rootId };
        var queue = new Queue<int>([rootId]);
        while (queue.Count > 0)
        {
            if (!childrenByParent.TryGetValue(queue.Dequeue(), out var childIds))
            {
                continue;
            }

            foreach (var childId in childIds.Where(collected.Add))
            {
                queue.Enqueue(childId);
            }
        }

        return collected;
    }

    private static void Resequence(IReadOnlyList<AbwabTemplateNode> ordered, DateTimeOffset now)
    {
        for (var index = 0; index < ordered.Count; index++)
        {
            var node = ordered[index];
            if (node.OrderValue == index + 1)
            {
                continue;
            }

            node.OrderValue = index + 1;
            node.UpdatedAtUtc = now;
        }
    }

    // Its own translation, like EfAbwabRelationsWriter's: a bare SaveChangesAsync would let 23505
    // cross the seam as a 500 instead of the 409 the duplicate-sibling-name rule owes.
    private async Task SaveTranslatingDuplicateNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new AbwabTemplateNodeDuplicateNameException();
        }
    }

    private static AbwabTemplateNodeDto ToDto(AbwabTemplateNode node) =>
        new(node.Id,
            node.ParentNodeId,
            node.Name,
            node.Description,
            node.RepresentativeAyahText,
            node.Aliases,
            node.OrderValue);
}
