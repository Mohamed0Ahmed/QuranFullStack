namespace QuranDashboard.Infrastructure.Access;

// Not validated at startup: AppSecret is legitimately absent on a fresh clone, so these options are
// validated on first use instead (see LogtoManagementApiUserProfileSource.EnsureConfigured).
public sealed class LogtoManagementApiOptions
{
    public const string SectionName = "Auth:ManagementApi";

    public string Endpoint { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public string AppSecret { get; set; } = string.Empty;
}
