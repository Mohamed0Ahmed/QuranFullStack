using QuranDashboard.Tests.TestSupport.Access;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

public sealed class EmailIdentityNormalizerTests
{
    private readonly IEmailIdentityNormalizer _normalizer = new EmailIdentityNormalizer();

    public static IEnumerable<object[]> ValidVectors =>
        EmailIdentityContractVectors.Valid
            .Select(vector => new object[] { vector.Input, vector.ExpectedNormalized! });

    public static IEnumerable<object[]> InvalidVectors =>
        EmailIdentityContractVectors.Invalid
            .Select(vector => new object[] { vector.Input });

    public static IEnumerable<object[]> DuplicateNormalizedGroups =>
        EmailIdentityContractVectors.DuplicateNormalizedInputs
            .Select(group => new object[] { group.ToArray() });

    [Theory]
    [MemberData(nameof(ValidVectors))]
    public void ValidVector_NormalizesThroughOneSharedImplementation(string input, string expectedNormalized)
    {
        _normalizer.TryNormalize(input, out var normalized).Should().BeTrue();
        normalized.Should().Be(expectedNormalized);
    }

    [Theory]
    [MemberData(nameof(InvalidVectors))]
    public void InvalidVector_FailsClosedWithoutProducingAnIdentity(string input)
    {
        _normalizer.TryNormalize(input, out var normalized).Should().BeFalse();
        normalized.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(DuplicateNormalizedGroups))]
    public void DuplicateVectorGroup_NormalizesToOneSharedIdentity(string[] inputs)
    {
        var normalized = inputs.Select(input => _normalizer.Normalize(input)).ToArray();

        normalized.Should().HaveCountGreaterThanOrEqualTo(2);
        normalized.Distinct(StringComparer.Ordinal).Should().ContainSingle();
    }

    [Fact]
    public void Normalize_ThrowsForInvalidIdentity()
    {
        var act = () => _normalizer.Normalize("not-an-email");

        act.Should().Throw<InvalidEmailIdentityException>();
    }
}
