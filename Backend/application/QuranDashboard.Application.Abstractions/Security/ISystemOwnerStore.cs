using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Application.Abstractions.Security;

// Persistence port for System Owner membership. `ListTrackedAsync` returns tracked entities so an owner
// op's Deactivate/Reactivate persists on the shared unit of work; `IsActiveSystemOwnerAsync` is a no-track
// read for the authorization handler. There is no email/role/runtime fallback path here (FR-024).
public interface ISystemOwnerStore
{
    Task<IReadOnlyList<SystemOwnerMembership>> ListTrackedAsync(CancellationToken cancellationToken);

    Task<bool> IsActiveSystemOwnerAsync(string subject, CancellationToken cancellationToken);

    void Add(SystemOwnerMembership membership);
}
