using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

internal sealed class InvalidatingLinkingConfirmationWriter(
    EfLinkingConfirmationWriter inner,
    IAbwabCacheInvalidator invalidator) : ILinkingConfirmationWriter
{
    private readonly EfLinkingConfirmationWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<LinkingConfirmationWriteResult> ConfirmPreparedAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken)
    {
        var result = await _inner.ConfirmPreparedAsync(lease, cancellationToken);
        if (result is LinkingConfirmationWriteResult.Success { IsReplay: false, Result.IsNoOp: false })
        {
            _invalidator.InvalidateTree();
        }

        return result;
    }
}
