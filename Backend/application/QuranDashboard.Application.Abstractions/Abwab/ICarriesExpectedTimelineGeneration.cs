namespace QuranDashboard.Application.Abstractions.Abwab;

// Every mutation port/command and every actionable read MUST carry an ExpectedTimelineGeneration.
// The foundation contract/source coverage test fails if any Abwab request omits it.
public interface ICarriesExpectedTimelineGeneration
{
    ExpectedTimelineGeneration ExpectedTimelineGeneration { get; }
}
