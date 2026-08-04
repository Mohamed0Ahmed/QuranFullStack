using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

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

        if (template is not null)
        {
            _cache.Set(key, new StampedTemplate(generation, template));
        }

        return template;
    }

    private sealed record StampedList(long Generation, IReadOnlyList<AbwabTemplateSummaryDto> Templates);

    private sealed record StampedTemplate(long Generation, AbwabTemplateDto Template);
}
