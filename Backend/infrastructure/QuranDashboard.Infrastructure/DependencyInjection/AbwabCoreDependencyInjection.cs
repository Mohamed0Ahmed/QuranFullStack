using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.ServiceRegistration;

internal static class AbwabCoreDependencyInjection
{
    public static IServiceCollection AddAbwabCoreReads(this IServiceCollection services)
    {
        services.AddScoped<IAbwabCoreReadPort, EfAbwabCoreReadPort>();
        services.AddScoped<IManualProtectionReadPort, EfManualProtectionReadPort>();

        services.AddScoped<ISectionWriteStore, EfSectionWriteStore>();
        services.AddScoped<ICategoryTreeStore, EfCategoryTreeStore>();
        services.AddScoped<ICategorySearchAliasWriteStore, EfCategorySearchAliasWriteStore>();
        services.AddScoped<IManualProtectionWriteStore, EfManualProtectionWriteStore>();

        services.AddScoped<IAbwabRelationshipReadPort, EfAbwabRelationshipReadPort>();
        services.AddScoped<ICategoryRelationshipStore, EfCategoryRelationshipStore>();

        services.AddScoped<IAbwabTemplateReadPort, EfAbwabTemplateReadPort>();
        services.AddScoped<IDoorTemplateStore, EfDoorTemplateStore>();
        services.AddScoped<ITemplateNodeStore, EfTemplateNodeStore>();
        services.AddScoped<ITemplateNodeAliasStore, EfTemplateNodeAliasStore>();

        return services;
    }
}
