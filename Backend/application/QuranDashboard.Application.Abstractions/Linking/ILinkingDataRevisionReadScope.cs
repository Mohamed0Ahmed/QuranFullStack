namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingDataRevisionReadScope
{
    Task<TResult> ExecuteAsync<TResult>(
        int maximumAttempts,
        Func<long, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}

public sealed class LinkingDataRevisionReadRetryExhaustedException(Exception innerException)
    : Exception("The linking data revision read could not obtain a stable snapshot.", innerException);
