namespace QuranDashboard.Application.Abstractions.Abwab;

// Marker for any Abwab writer (mutation command/handler). The stabilization registry test enumerates
// every IAbwabWriter and fails if one is not registered against the AbwabWriteBarrier gate.
public interface IAbwabWriter
{
}
