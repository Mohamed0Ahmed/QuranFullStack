namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabInvalidPositionException : Exception
{
    public AbwabInvalidPositionException()
        : base("The requested position is outside the sibling range.")
    {
    }
}
