using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfManualProtectionWriteStore(QuranDashboardDbContext db) : IManualProtectionWriteStore
{
    public Task<ManualProtection?> FindActiveAsync(Guid categoryId, ManualProtectionType protectionType, CancellationToken cancellationToken) =>
        db.AbwabManualProtections.SingleOrDefaultAsync(
            p => p.CategoryId == categoryId && p.ProtectionType == protectionType && !p.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<ManualProtection>> FindActiveByCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await db.AbwabManualProtections.Where(p => p.CategoryId == categoryId && !p.IsDeleted).ToListAsync(cancellationToken);

    public void Add(ManualProtection protection) => db.AbwabManualProtections.Add(protection);
}
