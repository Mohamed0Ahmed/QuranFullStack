using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class AbwabCacheGeneration : IAbwabCacheInvalidator, IAbwabCacheValidators
{
    // A bare counter is not restart-safe: it resets to 0, and writes by other admins climb it back to a
    // value an idle client still holds from before the restart, whose next If-None-Match would false-match
    // and be answered 304 with stale data. The boot id makes cross-restart equality impossible, so a
    // restart costs every client exactly one refetch.
    private readonly string _bootId = Guid.NewGuid().ToString("N")[..8];

    private long _treeGeneration;
    private long _templatesGeneration;

    public void InvalidateTree() => Interlocked.Increment(ref _treeGeneration);

    public void InvalidateTemplates() => Interlocked.Increment(ref _templatesGeneration);

    // Resource-scoped by prefix so a list validator can never match a detail request, and opaque: nothing
    // in it is derived from row data, which is what keeps AbwabTreeDto.Version out of concurrency's way.
    public string TreeETag() => $"\"abwab-tree-{_bootId}-{Interlocked.Read(ref _treeGeneration)}\"";

    public string TemplatesListETag() => $"\"abwab-templates-{_bootId}-{Interlocked.Read(ref _templatesGeneration)}\"";

    public string TemplateETag(int templateId) =>
        $"\"abwab-template-{templateId}-{_bootId}-{Interlocked.Read(ref _templatesGeneration)}\"";

    // The stamp a cache entry carries. Callers capture it BEFORE loading (see the cached readers) so a
    // write that commits mid-load stamps the entry with the older generation and kills it on arrival.
    public long TreeGeneration() => Interlocked.Read(ref _treeGeneration);

    public long TemplatesGeneration() => Interlocked.Read(ref _templatesGeneration);
}
