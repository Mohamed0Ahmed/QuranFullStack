namespace QuranDashboard.Application.Abstractions.Abwab;

// One entry in a bulk-move/bulk-archive request: every touched row carries its own concurrency
// token, since the locked decision is all-or-nothing — one stale row fails the whole operation.
public sealed record AbwabBulkDoorRef(int DoorId, uint Version);
