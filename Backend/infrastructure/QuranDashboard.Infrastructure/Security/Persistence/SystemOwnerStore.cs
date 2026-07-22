using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Infrastructure.Security.Persistence;

public sealed class SystemOwnerStore(QuranDashboardDbContext db) : ISystemOwnerStore
{
    public async Task<IReadOnlyList<SystemOwnerMembership>> ListTrackedAsync(CancellationToken cancellationToken) =>
        await db.Set<SystemOwnerMembership>().ToListAsync(cancellationToken);

    public Task<bool> IsActiveSystemOwnerAsync(string subject, CancellationToken cancellationToken) =>
        db.Set<SystemOwnerMembership>()
            .AsNoTracking()
            .AnyAsync(owner => owner.Subject == subject && owner.IsActive && owner.IsAccountEnabled, cancellationToken);

    public void Add(SystemOwnerMembership membership) => db.Set<SystemOwnerMembership>().Add(membership);
}
