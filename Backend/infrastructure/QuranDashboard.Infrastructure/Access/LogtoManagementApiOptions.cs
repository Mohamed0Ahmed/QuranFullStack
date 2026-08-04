namespace QuranDashboard.Infrastructure.Access;

public sealed class LogtoManagementApiOptions
{
    public const string SectionName = "Auth:ManagementApi";

    public string Endpoint { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public string AppSecret { get; set; } = string.Empty;
}
