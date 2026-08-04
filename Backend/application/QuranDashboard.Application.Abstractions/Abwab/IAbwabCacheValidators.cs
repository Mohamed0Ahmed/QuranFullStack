namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabCacheValidators
{
    string TreeETag();

    string TemplatesListETag();

    string TemplateETag(int templateId);
}
