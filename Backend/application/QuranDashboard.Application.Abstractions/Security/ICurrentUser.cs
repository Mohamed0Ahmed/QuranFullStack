namespace QuranDashboard.Application.Abstractions.Security;

// The authenticated caller's Logto `sub` claim — the stable identity key that joins a Logto account
// to its local Users row.
public interface ICurrentUser
{
    // Fail-closed: throws if accessed outside an authenticated request (behind [Authorize]).
    string Sub { get; }
}
