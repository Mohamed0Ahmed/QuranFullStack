using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

public sealed class EfManualProtectionReadPort(QuranDashboardDbContext db) : IManualProtectionReadPort
{
    public async Task<CategoryProtectionContextDto?> GetProtectionContextAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        // No IsDeleted filter here: this must still resolve for a soft-deleted CategoryId (T028).
        var category = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.AncestorIds,
                c.OrdinaryProtectionActorSubject,
                c.OrdinaryProtectionLastEditedAtUtc,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return null;
        }

        var candidateIds = new List<Guid>(category.AncestorIds) { category.CategoryId };

        var activeProtections = await db.AbwabManualProtections
            .AsNoTracking()
            .Where(p => !p.IsDeleted && candidateIds.Contains(p.CategoryId))
            .Select(p => new ActiveManualProtectionRecordDto(p.ManualProtectionId, p.CategoryId, p.ProtectionType, p.ProtectionScope, p.Version))
            .ToListAsync(cancellationToken);

        var revision = await db.AbwabRevisionStates
            .AsNoTracking()
            .SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken);

        return new CategoryProtectionContextDto(
            category.CategoryId,
            category.AncestorIds,
            activeProtections,
            category.OrdinaryProtectionActorSubject,
            category.OrdinaryProtectionLastEditedAtUtc,
            revision.TimelineGeneration);
    }
}
