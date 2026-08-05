using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class EmailIdentityPreflight(
    QuranDashboardDbContext db,
    IEmailIdentityNormalizer normalizer) : IEmailIdentityPreflight
{
    private const string UserIdentityQuery = """
        SELECT id, email, normalized_email
        FROM users
        ORDER BY id;
        """;

    public async Task<EmailIdentityScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        var users = await ReadUsersAsync(cancellationToken);
        return BuildResult(users);
    }

    public async Task<int> BackfillAsync(CancellationToken cancellationToken)
    {
        var users = await ReadUsersAsync(cancellationToken);
        var result = BuildResult(users);
        if (!result.IsClean && result.InvalidUserIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Normalized email backfill found invalid user IDs: {string.Join(",", result.InvalidUserIds)}.");
        }

        if (result.Collisions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Normalized email backfill found collisions for: "
                + string.Join(",", result.Collisions.Select(collision => collision.NormalizedEmail)));
        }

        var updates = new List<EmailIdentityUpdate>();
        foreach (var user in users)
        {
            var normalizedEmail = normalizer.Normalize(user.Email);
            if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            {
                updates.Add(new EmailIdentityUpdate(user.Id, normalizedEmail));
            }
        }

        if (updates.Count > 0)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            foreach (var update in updates)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE users SET normalized_email = {update.NormalizedEmail} WHERE id = {update.UserId};",
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return updates.Count;
    }

    private async Task<IReadOnlyList<EmailIdentityRow>> ReadUsersAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = UserIdentityQuery;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var users = new List<EmailIdentityRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(new EmailIdentityRow(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            return users;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    private EmailIdentityScanResult BuildResult(IReadOnlyList<EmailIdentityRow> users)
    {
        var invalidUserIds = new List<int>();
        var missingNormalizedEmailUserIds = new List<int>();
        var mismatchedNormalizedEmailUserIds = new List<int>();
        var candidates = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        foreach (var user in users)
        {
            if (!normalizer.TryNormalize(user.Email, out var normalizedEmail))
            {
                invalidUserIds.Add(user.Id);
                continue;
            }

            if (!candidates.TryGetValue(normalizedEmail!, out var userIds))
            {
                userIds = [];
                candidates.Add(normalizedEmail!, userIds);
            }

            userIds.Add(user.Id);

            if (string.IsNullOrWhiteSpace(user.NormalizedEmail))
            {
                missingNormalizedEmailUserIds.Add(user.Id);
            }
            else if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            {
                mismatchedNormalizedEmailUserIds.Add(user.Id);
            }
        }

        var collisions = candidates
            .Where(candidate => candidate.Value.Count > 1)
            .OrderBy(candidate => candidate.Key, StringComparer.Ordinal)
            .Select(candidate => new NormalizedEmailCollision(candidate.Key, candidate.Value))
            .ToArray();

        return new EmailIdentityScanResult(
            users.Count,
            invalidUserIds,
            missingNormalizedEmailUserIds,
            mismatchedNormalizedEmailUserIds,
            collisions);
    }

    private sealed record EmailIdentityRow(int Id, string Email, string? NormalizedEmail);

    private sealed record EmailIdentityUpdate(int UserId, string NormalizedEmail);
}
