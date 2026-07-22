using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// SavingChanges guard — layer 1 of the write kernel. Runs before every SaveChanges and enforces, on the
// tracked graph:
//   * no-ChangeSet write rejection — any IAbwabAuditable mutation requires a tracked ChangeSet in the
//     same unit of work;
//   * physical-delete rejection / soft-delete enforcement — an IAbwabAuditable in Deleted state is
//     refused unless the sealed, default-deny personal-delete policy allows that exact type.
// This is distinct from the forbidden-write-API bypass gate (a separate source/architecture test): this
// guard runs at execution time; the gate runs at build time.
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

        // A physical delete is rejected unless the sealed, default-deny personal-delete policy allows that
        // exact type; a permitted personal delete is outside the product-audit envelope, so it is exempt
        // from both this rejection and the ChangeSet requirement below.
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
