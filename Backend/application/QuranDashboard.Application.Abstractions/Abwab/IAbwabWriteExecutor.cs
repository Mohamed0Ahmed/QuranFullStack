namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabWriteExecutor
{
    Task<AbwabCommitResult> ExecuteAsync(AbwabWriteRequest request, CancellationToken cancellationToken);
}
