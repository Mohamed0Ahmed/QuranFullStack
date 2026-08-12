namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingIdempotencyConflictException : Exception
{
    public LinkingIdempotencyConflictException()
        : base("The idempotency key belongs to a different linking confirmation attempt.")
    {
    }
}
