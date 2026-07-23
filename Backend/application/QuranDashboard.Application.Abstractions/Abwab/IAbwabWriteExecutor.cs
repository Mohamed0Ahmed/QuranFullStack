namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabWriteExecutor
{
    Task<AbwabCommitResult> ExecuteAsync(AbwabWriteRequest request, CancellationToken cancellationToken);

    // Runs a domain operation inside the same barrier/revision lock, supporting idempotent no-ChangeSet
    // outcomes and in-transaction structural (TreeRevision) validation — used by the 029 category/section/
    // protection writers.
    Task<AbwabAuditedOperationResult> ExecuteAsync(AbwabAuditedOperationRequest request, CancellationToken cancellationToken);
}
