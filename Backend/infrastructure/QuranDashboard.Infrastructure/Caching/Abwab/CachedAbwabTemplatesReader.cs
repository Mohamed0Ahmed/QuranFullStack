using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

// List and details share one generation, so a node edit on template A also invalidates B's cached
// detail — one counter instead of a per-id registry, an admin-scale trade.
internal sealed class CachedAbwabTemplatesReader(
    EfAbwabTemplatesReader inner,
    IMemoryCache cache,
    AbwabCacheGeneration generations) : IAbwabTemplatesReader
{
    private const string ListKey = "abwab:templates";

    private readonly EfAbwabTemplatesReader _inner = inner;
    private readonly IMemoryCache _cache = cache;
    private readonly AbwabCacheGeneration _generations = generations;

    public async Task<IReadOnlyList<AbwabTemplateSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var generation = _generations.TemplatesGeneration();

        if (_cache.TryGetValue(ListKey, out StampedList? cached) && cached is not null && cached.Generation == generation)
        {
            return cached.Templates;
        }

        var templates = await _inner.GetAllAsync(cancellationToken);
        _cache.Set(ListKey, new StampedList(generation, templates));
        return templates;
    }

    public async Task<AbwabTemplateDto?> GetAsync(int templateId, CancellationToken cancellationToken)
    {
        var key = $"abwab:template:{templateId}";
        var generation = _generations.TemplatesGeneration();

        if (_cache.TryGetValue(key, out StampedTemplate? cached) && cached is not null && cached.Generation == generation)
        {
            return cached.Template;
        }

        var template = await _inner.GetAsync(templateId, cancellationToken);

        // A miss is never cached: template ids come from the caller, so caching absences would let an id
        // probe grow the key space without bound. Present entries are bounded by the templates admins own.
        if (template is not null)
        {
            _cache.Set(key, new StampedTemplate(generation, template));
        }

        return template;
    }

    private sealed record StampedList(long Generation, IReadOnlyList<AbwabTemplateSummaryDto> Templates);

    private sealed record StampedTemplate(long Generation, AbwabTemplateDto Template);
}
