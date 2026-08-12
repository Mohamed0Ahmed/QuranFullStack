namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingPreflightStaleException : Exception
{
    public LinkingPreflightStaleException()
        : base("The supplied preflight token no longer matches the door's confirmed state.")
    {
    }
}
