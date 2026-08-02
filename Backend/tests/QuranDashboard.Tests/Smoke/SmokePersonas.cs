namespace QuranDashboard.Tests.Smoke;

// An enum rather than a record set: a persona carries exactly one datum (its sub, absent for the
// anonymous case), and a closed enum lets a caller switch over it exhaustively.
internal enum SmokePersona
{
    Anonymous,
    AuthenticatedUnknown,
    Owner,
}

internal static class SmokePersonas
{
    public const string UnknownSub = "smoke-unknown";

    // Only this sub's fake profile email matches the configured Owner-bootstrap email, so it alone
    // takes the bootstrap path to Active/Owner.
    public const string OwnerSub = "smoke-owner";

    // Derived from the enum rather than hand-listed: a new persona is then covered by cache eviction
    // automatically, instead of leaking a stale role whenever someone updates SubFor and forgets a
    // parallel array.
    public static IReadOnlyList<string> TokenBearingSubs { get; } =
        Enum.GetValues<SmokePersona>().Select(SubFor).OfType<string>().ToArray();

    public static string? SubFor(SmokePersona persona) => persona switch
    {
        SmokePersona.Anonymous => null,
        SmokePersona.AuthenticatedUnknown => UnknownSub,
        SmokePersona.Owner => OwnerSub,
        _ => throw new ArgumentOutOfRangeException(nameof(persona), persona, "Unhandled smoke persona."),
    };
}
