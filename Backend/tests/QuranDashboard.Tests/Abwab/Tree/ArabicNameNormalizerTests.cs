using QuranDashboard.Domain.Abwab.Tree;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.Tree;

public sealed class ArabicNameNormalizerTests
{
    public static IEnumerable<object[]> CorpusCases() =>
        NormalizationCorpus.Cases.Select(c => new object[] { c.Name, c.Input, c.Expected });

    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void Normalize_MatchesCanonicalCorpus(string caseName, string input, string expected)
    {
        var actual = ArabicNameNormalizer.Normalize(input);

        actual.Should().Be(expected, "corpus case {0} defines the §5.1 contract", caseName);
    }

    [Fact]
    public void Normalize_DoesNotMapTehMarbutaToHeh()
    {
        ArabicNameNormalizer.Normalize("ة").Should().Be("ة");
        ArabicNameNormalizer.Normalize("ة").Should().NotBe("ه");
    }

    [Fact]
    public void Normalize_CorpusCoversTheFullFrozenMarkSet()
    {
        var frozenScalars = new (int Start, int End)[]
        {
            (0x0610, 0x061A),
            (0x064B, 0x065F),
            (0x0670, 0x0670),
            (0x06D6, 0x06DC),
            (0x06DF, 0x06E4),
            (0x06E7, 0x06E8),
            (0x06EA, 0x06ED),
            (0x0897, 0x089F),
            (0x08CA, 0x08E1),
            (0x08E3, 0x08FF),
            (0x10EFC, 0x10EFF),
        };

        foreach (var (start, end) in frozenScalars)
        {
            NormalizationCorpus.Cases.Should().Contain(c =>
                    ContainsScalar(c.Input, start) && !ContainsScalar(c.Expected, start),
                $"the corpus must exercise the first scalar of range U+{start:X4}-U+{end:X4}");
            NormalizationCorpus.Cases.Should().Contain(c =>
                    ContainsScalar(c.Input, end) && !ContainsScalar(c.Expected, end),
                $"the corpus must exercise the last scalar of range U+{start:X4}-U+{end:X4}");
        }
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        foreach (var testCase in NormalizationCorpus.Cases)
        {
            var once = ArabicNameNormalizer.Normalize(testCase.Input);
            var twice = ArabicNameNormalizer.Normalize(once);

            twice.Should().Be(once, "case {0} must be stable under repeated normalization", testCase.Name);
        }
    }

    [Fact]
    public void Normalize_NullOrEmpty_ReturnsEmpty()
    {
        ArabicNameNormalizer.Normalize(null).Should().Be(string.Empty);
        ArabicNameNormalizer.Normalize(string.Empty).Should().Be(string.Empty);
        ArabicNameNormalizer.Normalize("   ").Should().Be(string.Empty);
    }

    private static bool ContainsScalar(string value, int scalar)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (rune.Value == scalar)
            {
                return true;
            }
        }

        return false;
    }
}
