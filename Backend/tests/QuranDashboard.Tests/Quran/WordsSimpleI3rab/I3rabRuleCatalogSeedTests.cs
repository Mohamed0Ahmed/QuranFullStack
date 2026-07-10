using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class I3rabRuleCatalogSeedTests
{
    private readonly IReadOnlyList<I3rabRuleSeedRow> rows = new I3rabRuleCatalogSeed().Rows();

    public static TheoryData<string, string> CorrectedLabels => new()
    {
        { "STEM:ACC", "حرف نصب" },
        { "STEM:T", "ظرف زمان" },
        { "STEM:SUB", "حرف مصدري" },
        { "STEM:RES", "أداة حصر" },
        { "STEM:INTG", "اسم استفهام" },
        { "PREFIX:INTG", "همزة استفهام" },
        { "STEM:PRO", "لا الناهية" },
        { "STEM:AMD", "حرف استدراك" },
        { "STEM:SUP", "حرف زائد" },
        { "STEM:PREV", "ما الكافّة" },
        { "STEM:INC", "حرف ابتداء/استفتاح" },
        { "STEM:EXL", "حرف تفصيل" },
        { "STEM:INT", "حرف تفسير" },
        { "STEM:EXH", "حرف تحضيض" },
        { "STEM:SUR", "حرف فجاءة" },
        { "STEM:INL", "حروف مقطّعة (فواتح السور)" },
        { "PREFIX:EQ", "همزة التسوية" },
        { "SUFFIX:VOC", "ميم عوض عن حرف النداء" },
        { "PREFIX:COM", "واو المعية" },
        { "SUFFIX:P", "لام الجر" },
        { "STEM:N:GEN:1S", "اسم مجرور مضاف إلى ياء المتكلم" },
        { "PREFIX:REM", "حرف استئناف" },
        { "PREFIX:IMPV", "لام الأمر" }
    };

    public static TheoryData<string, string> DivineNameLabels => new()
    {
        { "STEM:PN:ALLAH:GEN", "لفظ الجلالة مجرور" },
        { "STEM:PN:ALLAH:NOM", "لفظ الجلالة مرفوع" },
        { "STEM:PN:ALLAH:ACC", "لفظ الجلالة منصوب" }
    };

    [Fact]
    public void Catalogue_has_exactly_142_rows_across_67_families()
    {
        rows.Should().HaveCount(142);
        rows.Select(row => row.RuleFamily).Distinct().Should().HaveCount(67);
    }

    [Fact]
    public void Every_row_is_approved_with_a_unique_signature_key()
    {
        rows.Should().OnlyContain(row => row.DefaultStatus == I3rabStatusMapping.Approved);
        rows.Select(row => row.SignatureKey).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_label_is_non_empty()
    {
        rows.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.I3rabArabic));
    }

    [Theory]
    [MemberData(nameof(CorrectedLabels))]
    [MemberData(nameof(DivineNameLabels))]
    public void Catalogue_carries_the_expected_corrected_label(string signatureKey, string expectedArabic)
    {
        new I3rabRuleCatalogSeed().TryGet(signatureKey, out var row).Should().BeTrue(
            $"signature '{signatureKey}' must exist in the catalogue");
        row.I3rabArabic.Should().Be(expectedArabic);
    }

    [Fact]
    public void Catalogue_uses_approved_simplified_particle_labels()
    {
        var catalog = new I3rabRuleCatalogSeed();

        catalog.TryGet("STEM:PRO", out var proRow).Should().BeTrue();
        proRow.I3rabArabic.Should().Be("لا الناهية");
        proRow.I3rabArabic.Should().NotBe("ضمير منفصل");

        catalog.TryGet("STEM:ACC", out var accRow).Should().BeTrue();
        accRow.I3rabArabic.Should().Be("حرف نصب");
        accRow.I3rabArabic.Should().NotContain("أخوات إنّ/النواصب");
    }

    [Fact]
    public void Catalogue_keeps_stem_and_suffix_pronoun_labels_unchanged()
    {
        var catalog = new I3rabRuleCatalogSeed();

        catalog.TryGet("STEM:PRON:3MS", out var stemRow).Should().BeTrue();
        stemRow.I3rabArabic.Should().Be("ضمير للغائب");

        catalog.TryGet("SUFFIX:PRON:3MS", out var suffixRow).Should().BeTrue();
        suffixRow.I3rabArabic.Should().Be("ضمير متصل للغائب");
    }
}
