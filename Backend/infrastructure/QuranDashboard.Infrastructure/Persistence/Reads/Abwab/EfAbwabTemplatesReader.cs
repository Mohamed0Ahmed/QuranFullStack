using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabTemplatesReader(QuranDashboardDbContext db) : IAbwabTemplatesReader
{
    public async Task<IReadOnlyList<AbwabTemplateSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        // One round trip, and the count is computed in SQL rather than by materializing a row per
        // node: what crosses the wire is one row per template, not one per node in the database.
        var rows = await db.AbwabTemplates.AsNoTracking()
            .Where(t => t.DeletedAtUtc == null)
            .OrderBy(t => t.CreatedAtUtc)
            .ThenBy(t => t.Id)
            .Select(t => new
            {
                t.Id,
                RootName = db.AbwabTemplateNodes
                    .Where(n => n.TemplateId == t.Id && n.ParentNodeId == null && n.DeletedAtUtc == null)
                    .Select(n => n.Name)
                    .FirstOrDefault(),
                DescendantCount = db.AbwabTemplateNodes
                    .Count(n => n.TemplateId == t.Id && n.ParentNodeId != null && n.DeletedAtUtc == null),
            })
            .ToListAsync(cancellationToken);

        // A rootless template is skipped, matching GetAsync's null. Unreachable today — create is the
        // only path and it always writes the root — but the name has no value to return, and the list
        // UI renders nothing else.
        return rows
            .Where(row => row.RootName is not null)
            .Select(row => new AbwabTemplateSummaryDto(row.Id, row.RootName!, row.DescendantCount))
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
