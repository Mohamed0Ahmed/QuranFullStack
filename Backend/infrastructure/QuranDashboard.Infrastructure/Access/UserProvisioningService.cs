using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

// First-login provisioning keyed by the Logto sub (decision 3). A new subject is created only from a
// server-verified email (never a client-supplied value) as Pending/no-role; the configured Owner email
// is the sole exception, provisioned or promoted to Owner/Active only when the email is also
// IdP-verified. A Disabled user is never auto-revived or promoted by login. Any role/status change here
// evicts the subject's cached role so the claims transformation observes it immediately.
public sealed class UserProvisioningService(
    QuranDashboardDbContext db,
    IExternalUserProfileSource profileSource,
    IUserRoleResolver roleResolver,
    IOptions<OwnerBootstrapOptions> bootstrapOptions) : IUserProvisioningService
{
    public async Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct)
    {
        var existing = await db.AccessUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
        if (existing is not null)
        {
            return await ReconcileExistingAsync(existing, logtoSub, ct);
        }

        var profile = await profileSource.GetProfileAsync(logtoSub, ct);
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new InvalidOperationException(
                $"Logto returned no primary email for subject '{logtoSub}'; a user cannot be provisioned " +
                "without a server-verified email, and a client-supplied value must never be substituted.");
        }

        return await CreateAsync(logtoSub, profile, ct);
    }

    private async Task<ProvisionedUser> ReconcileExistingAsync(User existing, string logtoSub, CancellationToken ct)
    {
        if (!IsConfiguredOwner(existing.Email))
        {
            return Project(existing, existing.Role?.Name);
        }

        // decision 3: never auto-revive or auto-promote a Disabled user on login.
        if (existing.Status == UserStatus.Disabled)
        {
            return Project(existing, existing.Role?.Name);
        }

        var ownerRole = await GetOwnerRoleAsync(ct);
        if (ownerRole is null || (existing.RoleId == ownerRole.Id && existing.Status == UserStatus.Active))
        {
            return Project(existing, existing.Role?.Name);
        }

        // decision 3: only promote a pre-existing owner-email user when their email is IdP-verified. This
        // profile fetch runs only on this rare owner-upgrade path, not on every reconcile call.
        var profile = await profileSource.GetProfileAsync(logtoSub, ct);
        if (!profile.EmailVerified)
        {
            return Project(existing, existing.Role?.Name);
        }

        var tracked = await db.AccessUsers.SingleAsync(u => u.Id == existing.Id, ct);
        tracked.RoleId = ownerRole.Id;
        tracked.Status = UserStatus.Active;
        tracked.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        roleResolver.Evict(logtoSub);
        return Project(tracked, ownerRole.Name);
    }

    private async Task<ProvisionedUser> CreateAsync(string logtoSub, ExternalUserProfile profile, CancellationToken ct)
    {
        // decision 3: an owner-email first login is provisioned as Owner/Active only when the IdP also
        // reports the email as verified; otherwise it is provisioned exactly like a normal user (Pending,
        // no role) and can be promoted later once verified.
        var ownerRole = IsConfiguredOwner(profile.Email!) && profile.EmailVerified
            ? await GetOwnerRoleAsync(ct)
            : null;

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            LogtoSub = logtoSub,
            Email = profile.Email!,
            UserName = profile.UserName,
            DisplayName = profile.DisplayName,
            Title = null,
            RoleId = ownerRole?.Id,
            Status = ownerRole is not null ? UserStatus.Active : UserStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AccessUsers.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
            if (ownerRole is not null)
            {
                // A negative role may have been cached for this subject before the row existed; drop it.
                roleResolver.Evict(logtoSub);
            }
            return Project(user, ownerRole?.Name);
        }
        catch (DbUpdateException ex)
        {
            // A concurrent first login for the same subject won the unique-index race. Detach our
            // failed insert and return the row that landed first; rethrow if it was some other failure.
            db.Entry(user).State = EntityState.Detached;
            var winner = await db.AccessUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
            if (winner is null)
            {
                // No row under our sub means the unique-index hit wasn't the same-sub race above — the
                // sub index would have produced a winner. It is the email unique index instead: a
                // subject deleted+recreated in Logto now presents a new sub with an already-registered
                // email. That is an expected business conflict, not a server fault.
                if (ex.InnerException is PostgresException { SqlState: "23505" })
                {
                    throw new UserProvisioningEmailConflictException(profile.Email!);
                }

                throw;
            }
            return Project(winner, winner.Role?.Name);
        }
    }

    private bool IsConfiguredOwner(string email)
    {
        var ownerEmail = bootstrapOptions.Value.BootstrapOwnerEmail;
        return !string.IsNullOrWhiteSpace(ownerEmail)
            && string.Equals(email, ownerEmail, StringComparison.OrdinalIgnoreCase);
    }

    private Task<Role?> GetOwnerRoleAsync(CancellationToken ct)
        => db.AccessRoles.AsNoTracking().SingleOrDefaultAsync(r => r.Name == RoleNames.Owner, ct);

    private static ProvisionedUser Project(User user, string? roleName)
        => new(user.LogtoSub, user.Email, user.DisplayName, user.Status, user.RoleId, roleName);
}
