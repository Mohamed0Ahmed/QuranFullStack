namespace QuranDashboard.Application.Abstractions.Abwab;

// An Abwab mutation command: it is a writer (must pass the barrier) AND must carry the generation
// contract. Implementing this is how a future 029+ command opts into both guarantees at once.
public interface IAbwabMutationCommand : IAbwabWriter, ICarriesExpectedTimelineGeneration
{
}
