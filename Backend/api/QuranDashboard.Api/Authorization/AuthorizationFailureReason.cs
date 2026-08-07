namespace QuranDashboard.Api.Authorization;

public enum AuthorizationFailureReason
{
    Unprovisioned,
    Inactive,
    MissingPermission,
    OwnerRequired,
    InfrastructureUnavailable,
}
