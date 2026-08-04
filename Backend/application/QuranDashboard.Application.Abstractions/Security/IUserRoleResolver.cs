namespace QuranDashboard.Application.Abstractions.Security;

public interface IUserRoleResolver
{
    Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct);

    void Evict(string logtoSub);
}
