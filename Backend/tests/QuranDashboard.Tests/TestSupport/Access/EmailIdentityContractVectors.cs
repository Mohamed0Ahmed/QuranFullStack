namespace QuranDashboard.Tests.TestSupport.Access;

internal sealed record EmailIdentityContractVector(string Input, string? ExpectedNormalized);

internal static class EmailIdentityContractVectors
{
    public static IReadOnlyList<EmailIdentityContractVector> Valid { get; } =
    [
        new("owner@example.test", "OWNER@EXAMPLE.TEST"),
        new(" Owner@example.test ", "OWNER@EXAMPLE.TEST"),
        new("TEACHER@Example.Test", "TEACHER@EXAMPLE.TEST"),
    ];

    public static IReadOnlyList<EmailIdentityContractVector> Invalid { get; } =
    [
        new("", null),
        new("   ", null),
        new("not-an-email", null),
        new("@example.test", null),
        new("owner@@example.test", null),
        new("Owner <owner@example.test>", null),
    ];

    public static IReadOnlyList<IReadOnlyList<string>> DuplicateNormalizedInputs { get; } =
    [
        ["owner@example.test", " Owner@Example.Test "],
        ["TEACHER@example.test", "teacher@EXAMPLE.TEST"],
    ];
}
