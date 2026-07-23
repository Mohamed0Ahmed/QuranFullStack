namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Integration seam for 032's Pending-aware reservation checker. 029 builds no request storage, so
// this seam is inert here: it never blocks a deletion. 032 installs the real implementation, which
// maps a positive reservation to abwab.category_reserved_by_pending.
public interface IDeletionReservationChecker
{
    Task<bool> IsReservedByPendingAsync(IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken);
}
