using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

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

        if (existing.Status == UserStatus.Disabled)
        {
            return Project(existing, existing.Role?.Name);
        }

        var ownerRole = await GetOwnerRoleAsync(ct);
        if (ownerRole is null || (existing.RoleId == ownerRole.Id && existing.Status == UserStatus.Active))
        {
            return Project(existing, existing.Role?.Name);
        }

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
                roleResolver.Evict(logtoSub);
            }
            return Project(user, ownerRole?.Name);
        }
        catch (DbUpdateException ex)
        {
            db.Entry(user).State = EntityState.Detached;
            var winner = await db.AccessUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
            if (winner is null)
            {
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
