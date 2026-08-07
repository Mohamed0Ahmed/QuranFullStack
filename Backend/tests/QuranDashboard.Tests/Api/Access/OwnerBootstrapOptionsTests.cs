using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

public sealed class OwnerBootstrapOptionsTests
{
    [Fact]
    public void Validate_NormalizesEveryConfiguredEmailAndCreatesAStableFingerprint()
    {
        var options = new OwnerBootstrapOptions
        {
            Emails = [" Owner@Example.Test ", "owner-two@example.test"],
        };

        var result = Validator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
        options.NormalizedEmails.Should().BeEquivalentTo(
            "OWNER@EXAMPLE.TEST",
            "OWNER-TWO@EXAMPLE.TEST");
        options.ConfigurationFingerprint.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Theory]
    [MemberData(nameof(InvalidOwnerLists))]
    public void Validate_InvalidOrDuplicateOwnerList_FailsClosed(string[] emails)
    {
        var options = new OwnerBootstrapOptions { Emails = emails.ToList() };

        var result = Validator().Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    public static IEnumerable<object[]> InvalidOwnerLists =>
    [
        [Array.Empty<string>()],
        [new[] { "not-an-email" }],
        [new[] { "owner@example.test", " Owner@Example.Test " }],
    ];

    private static OwnerBootstrapOptionsValidator Validator()
        => new(new EmailIdentityNormalizer());
}
