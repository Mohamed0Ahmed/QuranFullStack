namespace QuranDashboard.Domain.Abwab.Concurrency;

public enum AbwabWriteBarrierState
{
    // Seeded state: writers may proceed. Persisted as int; do not renumber.
    Writable = 0,

    // Global freeze (e.g. during a restore/stabilization window): every writer is refused.
    Stabilizing = 1,
}
