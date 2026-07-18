namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// Exposes the authenticated caller's Logto <c>sub</c> claim — the stable identity key that joins a
/// Logto account to its local <c>Users</c> row. Resolve it ONLY inside an authenticated request
/// (behind <c>[Authorize]</c>); outside one there is no authenticated subject and the implementation
/// throws.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The authenticated Logto <c>sub</c> claim. Throws if accessed outside an authenticated request.
    /// </summary>
    string Sub { get; }
}
