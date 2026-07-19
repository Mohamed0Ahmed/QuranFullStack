namespace QuranDashboard.Infrastructure.Access;

/// <summary>
/// Bound configuration for the Logto Management API, the trusted server-side source of a user's
/// verified email/profile. <see cref="AppSecret"/> is a secret and is legitimately absent on a fresh
/// clone, so these options are NOT validated at startup; they are validated on first use instead
/// (see <see cref="LogtoManagementApiUserProfileSource"/>).
/// </summary>
public sealed class LogtoManagementApiOptions
{
    public const string SectionName = "Auth:ManagementApi";

    /// <summary>Logto tenant endpoint, e.g. <c>https://&lt;tenant&gt;.logto.app</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Management API resource indicator, typically <c>https://&lt;tenant-id&gt;.logto.app/api</c>.</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Machine-to-machine application id used for the client-credentials token.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Machine-to-machine application secret; supplied via user-secrets or environment.</summary>
    public string AppSecret { get; set; } = string.Empty;
}
