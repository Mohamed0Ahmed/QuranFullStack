namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingDuplicateContributionException : Exception
{
    public LinkingDuplicateContributionException()
        : base("A live contribution already exists for this door and source identity.")
    {
    }
}
