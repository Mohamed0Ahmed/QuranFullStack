namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingPageOutOfRangeException(int page)
    : Exception("The requested linking page is outside the logical view.")
{
    public int Page { get; } = page;
}
