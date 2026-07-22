namespace QuranDashboard.Application.Abstractions.Abwab;

// An actionable read (a read whose result seeds a follow-up mutation) must also carry the generation
// contract so a stale snapshot cannot silently drive a stale write.
public interface IAbwabActionableRead : ICarriesExpectedTimelineGeneration
{
}
