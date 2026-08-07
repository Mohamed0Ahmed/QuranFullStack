namespace QuranDashboard.Tests.Smoke;

using QuranDashboard.Tests.TestSupport.Access;

// An enum rather than a record set: a persona carries exactly one datum (its sub, absent for the
// anonymous case), and a closed enum lets a caller switch over it exhaustively.
internal enum SmokePersona
{
    Anonymous,
    InvalidToken,
    AuthenticatedUnknown,
    Pending,
    Disabled,
    ReadOnly,
    ExactPermission,
    NeighboringPermission,
    Owner,
    DisabledOwner,
    ClaimSmuggling,
}

internal static class SmokePersonas
{
    public const string UnknownSub = "smoke-unknown";
    public const string OwnerSub = "smoke-owner";

    public static IReadOnlyList<SmokePersona> All { get; } = Enum.GetValues<SmokePersona>();

    public static TestAccessPersona DefinitionFor(SmokePersona persona) =>
        TestAccessPersonas.For(persona.ToString());

    public static string? SubFor(SmokePersona persona) => DefinitionFor(persona).Sub;

    public static IReadOnlyDictionary<string, object> ClaimsFor(SmokePersona persona) =>
        DefinitionFor(persona).TokenClaims;
}
