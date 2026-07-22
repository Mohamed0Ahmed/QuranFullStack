using QuranDashboard.Domain.Abwab.Concurrency;

namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabStabilizationActiveException(AbwabWriteBarrierState state)
    : Exception($"Abwab write barrier is stabilizing (state: {state}); owner/permission writes are refused.")
{
    public string Code => AbwabConflictCodes.StabilizationActive;

    public AbwabWriteBarrierState State { get; } = state;
}
