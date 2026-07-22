using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Security;

// Computes a caller's effective permission codes for `/me` and the policy handlers, so all layers converge
// on the committed winner (FR-028). Sources: granted direct (subject) assignments, granted role
// assignments, the always-on DashboardAdminBaseline for the Admin role, and the implicit SystemOwnerOnly
// codes held by an active System Owner. Cached per subject; the cache is invalidated post-commit on any real
// grant/revoke.
public sealed class EffectivePermissionResolver(
    IPermissionAssignmentStore assignments,
    ISystemOwnerStore owners,
    IEffectivePermissionCache cache)
{
    public async Task<IReadOnlyList<string>> ResolveAsync(string subject, string? roleName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(subject, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var effective = new HashSet<string>(StringComparer.Ordinal);

        var granted = await assignments.ListGrantedAsync(cancellationToken);
        foreach (var assignment in granted)
        {
            var matchesSubject = assignment.TargetKind == PermissionTargetKind.Subject
                && string.Equals(assignment.TargetKey, subject, StringComparison.Ordinal);
            var matchesRole = assignment.TargetKind == PermissionTargetKind.Role
                && roleName is not null
                && string.Equals(assignment.TargetKey, roleName, StringComparison.Ordinal);

            if (matchesSubject || matchesRole)
            {
                effective.Add(assignment.PermissionCode);
            }
        }

        if (string.Equals(roleName, RoleNames.Admin, StringComparison.Ordinal))
        {
            foreach (var code in PermissionCatalogue.BaselineCodes)
            {
                effective.Add(code);
            }
        }

        if (await owners.IsActiveSystemOwnerAsync(subject, cancellationToken))
        {
            foreach (var code in PermissionCatalogue.SystemOwnerOnlyCodes)
            {
                effective.Add(code);
            }
        }

        var result = effective.OrderBy(code => code, StringComparer.Ordinal).ToList();
        await cache.SetAsync(subject, result, cancellationToken);
        return result;
    }
}
