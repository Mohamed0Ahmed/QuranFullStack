using QuranDashboard.Domain.Abwab.Concurrency;

namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabWriteBarrierClosedException(AbwabWriteBarrierState state)
    : Exception($"Abwab write barrier is not writable (state: {state}).")
{
    public string Code => AbwabConflictCodes.WriteBarrierClosed;

    public AbwabWriteBarrierState State { get; } = state;
}
