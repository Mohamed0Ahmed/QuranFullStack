using QuranDashboard.Application.Abstractions.Abwab.Core;

namespace QuranDashboard.Infrastructure.Abwab.Persistence;

public sealed class InertDeletionReservationChecker : IDeletionReservationChecker
{
    public Task<bool> IsReservedByPendingAsync(IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
