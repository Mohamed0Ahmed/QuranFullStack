using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class UserProvisioningService(
    QuranDashboardDbContext db,
    IExternalUserProfileSource profileSource,
    IOwnerReconciliationService ownerReconciliationService,
    IOwnerBootstrapConfigurationSource bootstrapConfigurationSource,
    IEmailIdentityNormalizer emailIdentityNormalizer) : IUserProvisioningService
{
    public async Task<ProvisionedUser> GetOrCreateAsync(AuthenticatedInteractiveIdentity identity, CancellationToken ct)
    {
        var logtoSub = identity.Sub;
        var existing = await db.AccessUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.LogtoSub == logtoSub, ct);
        if (existing is not null)
        {
            if (bootstrapConfigurationSource.GetCurrent().NormalizedEmails.Contains(existing.NormalizedEmail))
            {
                await ownerReconciliationService.ReconcileInteractiveSignInAsync(identity, ct);
            }

            return await LoadProvisionedUserAsync(logtoSub, ct);
        }

        var profile = await profileSource.GetProfileAsync(logtoSub, ct);
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new InvalidOperationException(
                $"Logto returned no primary email for subject '{logtoSub}'; a user cannot be provisioned " +
                "without provider identity data, and a client-supplied value must never be substituted.");
        }

        var user = await CreateAsync(logtoSub, profile, ct);
        if (bootstrapConfigurationSource.GetCurrent().NormalizedEmails.Contains(user.NormalizedEmail))
        {
            await ownerReconciliationService.ReconcileInteractiveSignInAsync(identity, ct);
        }

        return await LoadProvisionedUserAsync(logtoSub, ct);
    }

    private async Task<User> CreateAsync(string logtoSub, ExternalUserProfile profile, CancellationToken ct)
    {
        var normalizedEmail = emailIdentityNormalizer.Normalize(profile.Email!);
        var normalizedCollision = await db.AccessUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, ct);
        if (normalizedCollision is not null && normalizedCollision.LogtoSub != logtoSub)
        {
            throw new UserProvisioningEmailConflictException(profile.Email!);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            LogtoSub = logtoSub,
            Email = profile.Email!,
            NormalizedEmail = normalizedEmail,
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
            return user;
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
            return winner;
        }
    }

    private async Task<ProvisionedUser> LoadProvisionedUserAsync(string logtoSub, CancellationToken ct)
    {
        var user = await db.AccessUsers
            .AsNoTracking()
            .Include(candidate => candidate.Role)
            .Include(candidate => candidate.UserPermissions)
            .ThenInclude(grant => grant.Permission)
            .SingleAsync(candidate => candidate.LogtoSub == logtoSub, ct);
        return Project(user);
    }

    private static ProvisionedUser Project(User user)
    {
        var isOwner = user.Role?.Name == RoleNames.Owner;
        var permissions = user.Status == UserStatus.Active && !isOwner
            ? user.UserPermissions
                .Where(grant => grant.Permission.RetiredAtUtc is null)
                .OrderBy(grant => grant.Permission.DisplayOrder)
                .ThenBy(grant => grant.Permission.Code, StringComparer.Ordinal)
                .Select(grant => grant.Permission.Code)
                .ToArray()
            : [];

        return new ProvisionedUser(
            user.LogtoSub,
            user.Email,
            user.DisplayName,
            user.Status,
            user.RoleId,
            isOwner,
            permissions,
            isOwner ? RoleNames.Owner : null);
    }
}
