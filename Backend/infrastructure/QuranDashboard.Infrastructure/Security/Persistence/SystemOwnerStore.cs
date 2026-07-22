using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Infrastructure.Security.Persistence;

// Shares the request-scoped DbContext with the security-audit executor, so tracked reads/adds enlist in the
// executor's transaction and are persisted by its single SaveChanges. `IsActiveSystemOwnerAsync` is a
// no-track read used by the authorization handler outside any write.
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
