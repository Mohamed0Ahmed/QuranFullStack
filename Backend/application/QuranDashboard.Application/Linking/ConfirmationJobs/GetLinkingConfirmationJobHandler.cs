using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class GetLinkingConfirmationJobHandler(ILinkingConfirmationJobStore store)
{
    public Task<LinkingConfirmationJobStatusDto?> HandleAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken) =>
        store.GetStatusAsync(actorUserId, jobId, cancellationToken);
}
