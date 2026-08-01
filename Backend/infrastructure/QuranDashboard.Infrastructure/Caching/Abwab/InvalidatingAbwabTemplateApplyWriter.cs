using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

// Apply reads templates and writes doors + aliases, so it bumps the TREE generation and not the
// templates one — the direction that surprises readers of the route's name.
internal sealed class InvalidatingAbwabTemplateApplyWriter(
    EfAbwabTemplateApplyWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabTemplateApplyWriter
{
    private readonly EfAbwabTemplateApplyWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<IReadOnlyList<AbwabDoorDto>> ApplyAsync(
        int templateId,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.ApplyAsync(templateId, targetDoorIds, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }
}
