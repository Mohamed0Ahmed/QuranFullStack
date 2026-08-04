namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabCacheInvalidator
{
    void InvalidateTree();

    void InvalidateTemplates();
}
