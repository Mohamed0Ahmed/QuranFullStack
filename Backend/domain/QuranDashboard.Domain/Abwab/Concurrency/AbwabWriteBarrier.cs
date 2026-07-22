namespace QuranDashboard.Domain.Abwab.Concurrency;

public sealed class AbwabWriteBarrier
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public AbwabWriteBarrierState State { get; set; }
}
