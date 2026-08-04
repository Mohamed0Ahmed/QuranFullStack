using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class InvalidatingAbwabSectionsWriter(
    EfAbwabSectionsWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabSectionsWriter
{
    private readonly EfAbwabSectionsWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<AbwabSectionDto> CreateAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.CreateAsync(name, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabSectionDto?> RenameAsync(int id, string name, uint expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.RenameAsync(id, name, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabSectionDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.DeleteAsync(id, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabSectionDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.ReorderAsync(id, position, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }
}
