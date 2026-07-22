using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Application.Abstractions.Security;

public interface ISystemOwnerStore
{
    Task<IReadOnlyList<SystemOwnerMembership>> ListTrackedAsync(CancellationToken cancellationToken);

    Task<bool> IsActiveSystemOwnerAsync(string subject, CancellationToken cancellationToken);

    void Add(SystemOwnerMembership membership);
}
