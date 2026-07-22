namespace QuranDashboard.Infrastructure.Abwab.Notifications;

public static class NotificationStorageDependencyInjection
{
    public static IServiceCollection AddNotificationStorage(this IServiceCollection services)
    {
        services.AddScoped<NotificationStorageWriter>();
        services.AddScoped<NotificationReadStateRepository>();

        return services;
    }
}
