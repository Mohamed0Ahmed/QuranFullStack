namespace QuranDashboard.Domain.Abwab.Concurrency;

// Global singleton gate. Seeded Writable at migration alongside the revision-state and generation-zero
// boundary. Every Abwab writer MUST row-lock and evaluate this barrier first (§6.2 step 3), before the
// AbwabRevisionState head (§6.2 step 4). A stabilization registry test fails if any writer lacks the gate.
public sealed class AbwabWriteBarrier
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public AbwabWriteBarrierState State { get; set; }
}
