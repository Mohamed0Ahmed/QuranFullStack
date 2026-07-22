namespace QuranDashboard.Domain.Abwab.Concurrency;

public enum AbwabWriteBarrierState
{
    // Persisted as int; do not renumber.
    Writable = 0,

    Stabilizing = 1,
}
