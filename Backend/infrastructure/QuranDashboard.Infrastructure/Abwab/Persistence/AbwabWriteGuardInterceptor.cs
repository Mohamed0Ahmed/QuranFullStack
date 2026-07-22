using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// Fail-closed SavingChanges guard: an IAbwabAuditable mutation requires a tracked ChangeSet, and a physical
// delete is refused unless the default-deny personal-delete policy allows that exact type.
public sealed class AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy personalDeletePolicy) : SaveChangesInterceptor
{
    private readonly AbwabPersonalDeletePolicy _personalDeletePolicy = personalDeletePolicy;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Guard(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var auditableMutations = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is IAbwabAuditable &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditableMutations.Count == 0)
        {
            return;
        }

        foreach (var deleted in auditableMutations.Where(entry => entry.State == EntityState.Deleted))
        {
            if (!_personalDeletePolicy.AllowsPhysicalDelete(deleted.Entity.GetType()))
            {
                throw new AbwabPhysicalDeleteRejectedException(deleted.Entity.GetType());
            }
        }

        var auditedMutations = auditableMutations
            .Where(entry => !IsPermittedPersonalDelete(entry))
            .ToList();

        if (auditedMutations.Count == 0)
        {
            return;
        }

        var hasTrackedChangeSet = context.ChangeTracker.Entries()
            .Any(entry => entry.Entity is ChangeSet && entry.State == EntityState.Added);

        if (!hasTrackedChangeSet)
        {
            throw new AbwabWriteWithoutChangeSetException();
        }
    }

    private bool IsPermittedPersonalDelete(EntityEntry entry) =>
        entry.State == EntityState.Deleted && _personalDeletePolicy.AllowsPhysicalDelete(entry.Entity.GetType());
}
