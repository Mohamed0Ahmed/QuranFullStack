namespace QuranDashboard.Application.Abstractions.Abwab;

// The write half of the abwab cache. Segregated from IAbwabCacheValidators so a writer can only
// invalidate and a controller can only validate. Both are implemented by one process-wide object.
public interface IAbwabCacheInvalidator
{
    void InvalidateTree();

    void InvalidateTemplates();
}
