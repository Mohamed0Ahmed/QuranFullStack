using QuranDashboard.Domain.Abwab.Concurrency;

namespace QuranDashboard.Application.Abstractions.Abwab;

// Raised when the global AbwabWriteBarrier is not Writable at the moment a writer takes it.
// Fail-closed: no write proceeds while the barrier is closed. Mapped to 409 / abwab.write_barrier_closed.
public sealed class AbwabWriteBarrierClosedException(AbwabWriteBarrierState state)
    : Exception($"Abwab write barrier is not writable (state: {state}).")
{
    public string Code => AbwabConflictCodes.WriteBarrierClosed;

    public AbwabWriteBarrierState State { get; } = state;
}
