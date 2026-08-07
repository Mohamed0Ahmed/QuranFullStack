using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.TestSupport.Access;

internal sealed record CurrentUserCollision(string NormalizedEmail, IReadOnlyList<User> Users);

internal static class CurrentUserCollisionScan
{
    public static IReadOnlyList<CurrentUserCollision> Find(
        IEnumerable<User> users,
        Func<string, string?> normalize)
    {
        return users
            .Select(user => (User: user, NormalizedEmail: normalize(user.Email)))
            .Where(candidate => candidate.NormalizedEmail is not null)
            .GroupBy(candidate => candidate.NormalizedEmail!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new CurrentUserCollision(
                group.Key,
                group.Select(candidate => candidate.User).ToArray()))
            .ToArray();
    }
}
