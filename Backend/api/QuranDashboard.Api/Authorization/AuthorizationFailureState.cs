namespace QuranDashboard.Api.Authorization;

public sealed class AuthorizationFailureState
{
    public AuthorizationFailureReason? Reason { get; private set; }

    public void Record(AuthorizationFailureReason reason)
    {
        Reason ??= reason;
    }
}
