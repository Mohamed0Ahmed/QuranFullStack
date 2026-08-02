namespace QuranDashboard.Application.Abstractions.Abwab;

// The read half of the abwab cache: the ETag a conditional GET compares against. Lives in
// Abstractions because the controllers consume it and must not reference Infrastructure.
public interface IAbwabCacheValidators
{
    string TreeETag();

    string TemplatesListETag();

    string TemplateETag(int templateId);
}
