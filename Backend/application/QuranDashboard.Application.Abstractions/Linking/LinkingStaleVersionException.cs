namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingStaleVersionException : Exception
{
    public LinkingStaleVersionException()
        : base("The linking entity was modified by another request; the supplied version is stale.")
    {
    }
}
