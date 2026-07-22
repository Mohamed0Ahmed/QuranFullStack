namespace QuranDashboard.Application.Abstractions.Security;

public interface ICurrentUser
{
    // Fail-closed: throws if accessed outside an authenticated request (behind [Authorize]).
    string Sub { get; }
}
