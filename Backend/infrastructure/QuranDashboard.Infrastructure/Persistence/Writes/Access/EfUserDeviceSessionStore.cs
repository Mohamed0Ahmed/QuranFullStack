using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Access;

public sealed class EfUserDeviceSessionStore(QuranDashboardDbContext db) : IUserDeviceSessionStore
{
    public async Task<IssuedUserDeviceSession> IssueAsync(
        string logtoSub,
        string? previousToken,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logtoSub);

        var userId = await db.AccessUsers
            .Where(user => user.LogtoSub == logtoSub)
            .Select(user => user.Id)
            .SingleAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousToken))
        {
            var previousHash = Hash(previousToken);
            await db.Set<UserDeviceSession>()
                .Where(session => session.TokenHash == previousHash && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(session => session.RevokedAtUtc, createdAtUtc),
                    cancellationToken);
        }

        var token = GenerateToken();
        var csrfToken = GenerateToken();
        var session = new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(token),
            CsrfTokenHash = Hash(csrfToken),
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

        db.Set<UserDeviceSession>().Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IssuedUserDeviceSession(session.Id, token, csrfToken, expiresAtUtc);
    }

    public async Task<ResolvedUserDeviceSession?> ResolveAsync(
        string token,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = Hash(token);
        return await db.Set<UserDeviceSession>()
            .AsNoTracking()
            .Where(session => session.TokenHash == tokenHash
                              && session.RevokedAtUtc == null
                              && session.ExpiresAtUtc > nowUtc)
            .Select(session => new ResolvedUserDeviceSession(
                session.Id,
                session.User.LogtoSub,
                session.ExpiresAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ValidateCsrfAsync(
        Guid sessionId,
        string csrfToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(csrfToken))
        {
            return Task.FromResult(false);
        }

        var csrfTokenHash = Hash(csrfToken);
        return db.Set<UserDeviceSession>()
            .AsNoTracking()
            .AnyAsync(
                session => session.Id == sessionId
                           && session.CsrfTokenHash == csrfTokenHash
                           && session.RevokedAtUtc == null
                           && session.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public async Task RevokeAsync(
        Guid sessionId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await db.Set<UserDeviceSession>()
            .Where(session => session.Id == sessionId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAtUtc, revokedAtUtc),
                cancellationToken);
    }

    private static string GenerateToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
