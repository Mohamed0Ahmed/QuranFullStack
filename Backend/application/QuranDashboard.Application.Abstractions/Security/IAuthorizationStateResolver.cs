namespace QuranDashboard.Application.Abstractions.Security;

public interface IAuthorizationStateResolver
{
    Task<AuthorizationState?> ResolveAsync(string logtoSub, CancellationToken cancellationToken);
}
