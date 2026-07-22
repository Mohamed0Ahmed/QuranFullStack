using QuranDashboard.Domain.Abwab.Concurrency;

namespace QuranDashboard.Application.Abstractions.Abwab;

// Raised on the security surface when the global AbwabWriteBarrier is not Writable at the moment an
// owner/permission write takes it. Fail-closed: no security write proceeds while the barrier is
// stabilizing. Mapped to 409 / abwab.stabilization_active.
public sealed class AbwabStabilizationActiveException(AbwabWriteBarrierState state)
    : Exception($"Abwab write barrier is stabilizing (state: {state}); owner/permission writes are refused.")
{
    public string Code => AbwabConflictCodes.StabilizationActive;

    public AbwabWriteBarrierState State { get; } = state;
}
