using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class AbwabCacheGeneration : IAbwabCacheInvalidator, IAbwabCacheValidators
{
    private readonly string _bootId = Guid.NewGuid().ToString("N")[..8];

    private long _treeGeneration;
    private long _templatesGeneration;

    public void InvalidateTree() => Interlocked.Increment(ref _treeGeneration);

    public void InvalidateTemplates() => Interlocked.Increment(ref _templatesGeneration);

    public string TreeETag() => $"\"abwab-tree-{_bootId}-{Interlocked.Read(ref _treeGeneration)}\"";

    public string TemplatesListETag() => $"\"abwab-templates-{_bootId}-{Interlocked.Read(ref _templatesGeneration)}\"";

    public string TemplateETag(int templateId) =>
        $"\"abwab-template-{templateId}-{_bootId}-{Interlocked.Read(ref _templatesGeneration)}\"";

    public long TreeGeneration() => Interlocked.Read(ref _treeGeneration);

    public long TemplatesGeneration() => Interlocked.Read(ref _templatesGeneration);
}
