using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class InvalidatingAbwabDoorsWriter(
    EfAbwabDoorsWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabDoorsWriter
{
    private readonly EfAbwabDoorsWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<AbwabDoorDto> CreateAsync(
        int? sectionId,
        int? parentId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.CreateAsync(sectionId, parentId, name, description, representativeAyahText, aliases, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabDoorDto?> EditAsync(
        int id,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.EditAsync(id, name, description, representativeAyahText, aliases, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabDoorDto?> MoveAsync(
        int id,
        int? targetSectionId,
        int? targetParentId,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.MoveAsync(id, targetSectionId, targetParentId, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabDoorDto?> ReorderAsync(
        int id,
        int position,
        AbwabReorderScope scope,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.ReorderAsync(id, position, scope, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<IReadOnlyList<AbwabDoorDto>> BulkMoveAsync(
        IReadOnlyList<AbwabBulkDoorRef> doors,
        int? targetSectionId,
        int? targetParentId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.BulkMoveAsync(doors, targetSectionId, targetParentId, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<IReadOnlyList<int>> BulkArchiveAsync(IReadOnlyList<AbwabBulkDoorRef> doors, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.BulkArchiveAsync(doors, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<bool> DeleteAsync(int id, uint expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.DeleteAsync(id, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<AbwabRestoredDoorDto?> RestoreAsync(int id, uint expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.RestoreAsync(id, expectedVersion, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }
}
