using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

/// <summary>
/// EF-backed first-login provisioning keyed by the Logto <c>sub</c>. An already-provisioned subject is
/// returned unchanged (Phase 1 does not refresh fields); a first-time subject is created only from a
/// server-verified email obtained via <see cref="IExternalUserProfileSource"/> — never a client-supplied
/// value — with <see cref="UserStatus.Pending"/> status and no role.
/// </summary>
public sealed class UserProvisioningService(
    QuranDashboardDbContext db,
    IExternalUserProfileSource profileSource) : IUserProvisioningService
{
    public async Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct)
    {
        var existing = await db.AccessUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
        if (existing is not null)
        {
            return Project(existing);
        }

        var profile = await profileSource.GetProfileAsync(logtoSub, ct);
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new InvalidOperationException(
                $"Logto returned no primary email for subject '{logtoSub}'; a user cannot be provisioned " +
                "without a server-verified email, and a client-supplied value must never be substituted.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            LogtoSub = logtoSub,
            Email = profile.Email,
            UserName = profile.UserName,
            DisplayName = profile.DisplayName,
            Title = null,
            RoleId = null,
            Status = UserStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.AccessUsers.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
            return Project(user);
        }
        catch (DbUpdateException)
        {
            // A concurrent first login for the same subject won the unique-index race. Detach our
            // failed insert and return the row that landed first; rethrow if it was some other failure.
            db.Entry(user).State = EntityState.Detached;
            var winner = await db.AccessUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
            if (winner is null)
            {
                throw;
            }
            return Project(winner);
        }
    }

    private static ProvisionedUser Project(User user)
        => new(user.LogtoSub, user.Email, user.DisplayName, user.Status, user.RoleId);
}
