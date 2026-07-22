using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Tests.Quran.Words;

// ArabicSearchQueryNormalizer is internal; visible here via InternalsVisibleTo on the Infrastructure project.
public sealed class ArabicSearchQueryNormalizerTests
{
    [Theory]
    [InlineData("بِسْمِ", "بسم")]    [InlineData("أإآ", "ااا")]    [InlineData("ABC ابراهيم", "abc ابراهيم")]    [InlineData("قال الله", "قال الله")]    public void Normalize_default_folds_strips_diacritics_lowercases_and_keeps_interior_spaces(
        string input,
        string expected)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("قال الله", "قالالله")]    [InlineData("بِسْمِ اللَّهِ", "بسمالله")]    public void Normalize_with_stripWhitespace_also_removes_interior_spaces(string input, string expected)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input, stripWhitespace: true);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("   ", true)]    [InlineData("ًْ", false)]    [InlineData("ًْ", true)]
    public void Normalize_returns_null_for_diacritics_only_or_whitespace_only_input(
        string? input,
        bool stripWhitespace)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input, stripWhitespace);

        result.Should().BeNull();
    }
}
