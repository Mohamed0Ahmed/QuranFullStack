using FluentAssertions;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class AllahLemmaMatcherTests
{
    [Theory]
    [InlineData("{ll~ah", true)]
    [InlineData("Al~ah", false)]
    [InlineData("ilAh", false)]
    [InlineData(null, false)]
    public void IsAllahLemma_matches_only_locked_buckwalter_form(string? lemma, bool expected)
    {
        AllahLemmaMatcher.IsAllahLemma(lemma).Should().Be(expected);
        AllahLemmaMatcher.LockedLemmaBuckwalter.Should().Be("{ll~ah");
    }

    [Theory]
    [InlineData("GEN", "genitive", "STEM:PN:ALLAH:GEN", "لفظ الجلالة مجرور")]
    [InlineData("NOM", "nominative", "STEM:PN:ALLAH:NOM", "لفظ الجلالة مرفوع")]
    [InlineData("ACC", "accusative", "STEM:PN:ALLAH:ACC", "لفظ الجلالة منصوب")]
    public void Allah_stem_signatures_resolve_to_divine_name_catalogue_labels(
        string features,
        string caseFeature,
        string expectedSignature,
        string expectedLabel)
    {
        var input = new I3rabSegmentInput(
            1, 1, 1, "STEM", "PN", features, caseFeature, null, null, IsAllahLemma: true, FormIsNull: false);

        var signature = SegmentSignatureBuilder.Build(input);
        signature.Should().Be(expectedSignature);

        var catalog = new I3rabRuleCatalogSeed();
        catalog.TryGet(signature, out var row).Should().BeTrue();
        row.I3rabArabic.Should().Be(expectedLabel);
        row.DefaultStatus.Should().Be(I3rabStatusMapping.Approved);
    }
}
