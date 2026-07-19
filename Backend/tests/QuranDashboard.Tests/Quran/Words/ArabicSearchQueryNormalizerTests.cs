using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// Unit coverage for the shared Arabic search-query normalizer (decision 5 (DRY) consolidation of the
/// Unique Words/Word Types normalizer and the formerly-duplicated Roots/Lemmas/Stems
/// <c>NormalizeArabicQuery</c> copies). <c>ArabicSearchQueryNormalizer</c> is <c>internal</c>; visible here
/// via <c>InternalsVisibleTo</c> on the Infrastructure project.
/// </summary>
public sealed class ArabicSearchQueryNormalizerTests
{
    [Theory]
    [InlineData("بِسْمِ", "بسم")] // diacritics (kasra/sukun) stripped
    [InlineData("أإآ", "ااا")] // hamza/madda-alef fold (the canonical folding case)
    [InlineData("ABC ابراهيم", "abc ابراهيم")] // lower-cased, interior space KEPT by default
    [InlineData("قال الله", "قال الله")] // multi-word: interior space kept unchanged
    public void Normalize_default_folds_strips_diacritics_lowercases_and_keeps_interior_spaces(
        string input,
        string expected)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("قال الله", "قالالله")] // interior space removed
    [InlineData("بِسْمِ اللَّهِ", "بسمالله")] // diacritics stripped AND whitespace stripped together
    public void Normalize_with_stripWhitespace_also_removes_interior_spaces(string input, string expected)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input, stripWhitespace: true);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("   ", true)] // whitespace-only
    [InlineData("ًْ", false)] // diacritics-only (tanwin fath + sukun): empty after stripping
    [InlineData("ًْ", true)]
    public void Normalize_returns_null_for_diacritics_only_or_whitespace_only_input(
        string? input,
        bool stripWhitespace)
    {
        var result = ArabicSearchQueryNormalizer.Normalize(input, stripWhitespace);

        result.Should().BeNull();
    }
}
