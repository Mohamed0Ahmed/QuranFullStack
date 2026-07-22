namespace QuranDashboard.Infrastructure.Abwab.Notifications;

// Infrastructure composition of the 028 US6 notification STORAGE. Concrete writer + repository only — NO
// public port/interface is introduced (032 owns surfaces and the event matrix, 033 calls the writer for
// restore events). Registered scoped so both share the request DbContext, which is exactly how the writer
// joins a caller's unit of work.
public static class NotificationStorageDependencyInjection
{
    public static IServiceCollection AddNotificationStorage(this IServiceCollection services)
    {
        services.AddScoped<NotificationStorageWriter>();
        services.AddScoped<NotificationReadStateRepository>();

        return services;
    }
}
