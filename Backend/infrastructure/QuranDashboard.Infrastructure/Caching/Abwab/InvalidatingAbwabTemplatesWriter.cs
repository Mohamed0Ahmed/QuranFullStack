using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

// The only seam that bumps templates and never the tree: template tables are not part of the snapshot.
internal sealed class InvalidatingAbwabTemplatesWriter(
    EfAbwabTemplatesWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabTemplatesWriter
{
    private readonly EfAbwabTemplatesWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<AbwabTemplateDto> CreateAsync(
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.CreateAsync(name, description, representativeAyahText, aliases, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }

    public async Task<bool> DeleteAsync(int templateId, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.DeleteAsync(templateId, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }

    public async Task<AbwabTemplateNodeDto> AddNodeAsync(
        int templateId,
        int parentNodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.AddNodeAsync(templateId, parentNodeId, name, description, representativeAyahText, aliases, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }

    public async Task<AbwabTemplateNodeDto?> EditNodeAsync(
        int nodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.EditNodeAsync(nodeId, name, description, representativeAyahText, aliases, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }

    public async Task<AbwabTemplateNodeDto?> ReorderNodeAsync(int nodeId, int position, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.ReorderNodeAsync(nodeId, position, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }

    public async Task<AbwabTemplateNodeDeleteResult> DeleteNodeAsync(int nodeId, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.DeleteNodeAsync(nodeId, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTemplates();
        }
    }
}
