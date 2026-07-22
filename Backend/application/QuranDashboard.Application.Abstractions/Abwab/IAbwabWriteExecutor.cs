namespace QuranDashboard.Application.Abstractions.Abwab;

// The single barrier-gated entry every Abwab writer runs through. Passing the AbwabWriteBarrier and
// carrying ExpectedTimelineGeneration are enforced here; a writer that does not go through this port
// is what the stabilization registry test is designed to catch.
public interface IAbwabWriteExecutor
{
    Task<AbwabCommitResult> ExecuteAsync(AbwabWriteRequest request, CancellationToken cancellationToken);
}
