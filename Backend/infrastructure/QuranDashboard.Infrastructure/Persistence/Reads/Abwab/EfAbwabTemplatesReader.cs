using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabTemplatesReader(QuranDashboardDbContext db) : IAbwabTemplatesReader
{
    public async Task<IReadOnlyList<AbwabTemplateSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        // One query for every template's root name and node count, never one per template — the
        // N+1 the tree reader's own count query avoids.
        var rows = await (
            from node in db.AbwabTemplateNodes.AsNoTracking()
            join template in db.AbwabTemplates.AsNoTracking() on node.TemplateId equals template.Id
            where template.DeletedAtUtc == null && node.DeletedAtUtc == null
            select new
            {
                node.TemplateId,
                node.ParentNodeId,
                node.Name,
                TemplateCreatedAtUtc = template.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.TemplateId)
            .Select(group => new
            {
                TemplateId = group.Key,
                Root = group.FirstOrDefault(row => row.ParentNodeId is null),
                DescendantCount = group.Count(row => row.ParentNodeId is not null),
                CreatedAtUtc = group.First().TemplateCreatedAtUtc,
            })
            // A rootless template is skipped, matching GetAsync's null. Unreachable today — create
            // is the only path and it always writes the root — but the name has no value to return,
            // and the list UI renders nothing else.
            .Where(entry => entry.Root is not null)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.TemplateId)
            .Select(entry => new AbwabTemplateSummaryDto(entry.TemplateId, entry.Root!.Name, entry.DescendantCount))
            .ToList();
    }

    public async Task<AbwabTemplateDto?> GetAsync(int templateId, CancellationToken cancellationToken)
    {
        var templateExists = await db.AbwabTemplates.AsNoTracking()
            .AnyAsync(t => t.Id == templateId && t.DeletedAtUtc == null, cancellationToken);
        if (!templateExists)
        {
            return null;
        }

        var nodes = await db.AbwabTemplateNodes.AsNoTracking()
            .Where(n => n.TemplateId == templateId && n.DeletedAtUtc == null)
            .OrderBy(n => n.ParentNodeId)
            .ThenBy(n => n.OrderValue)
            .ThenBy(n => n.Id)
            .Select(n => new AbwabTemplateNodeDto(
                n.Id,
                n.ParentNodeId,
                n.Name,
                n.Description,
                n.RepresentativeAyahText,
                n.Aliases,
                n.OrderValue))
            .ToListAsync(cancellationToken);

        var root = nodes.FirstOrDefault(n => n.ParentNodeId is null);
        return root is null ? null : new AbwabTemplateDto(templateId, root.Name, nodes);
    }
}
