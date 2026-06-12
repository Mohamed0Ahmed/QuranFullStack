using FluentAssertions;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class I3rabAssemblerTests
{
    [Fact]
    public void Assemble_looks_up_catalogue_labels_with_approved_status()
    {
        var catalog = new InMemoryCatalog(
        [
            new I3rabRuleSeedRow("PREFIX:P", "P.PREFIX", "حرف جر", I3rabStatusMapping.Approved, null, 1),
            new I3rabRuleSeedRow("STEM:N:GEN", "N.GEN", "اسم مجرور", I3rabStatusMapping.Approved, null, 2),
            new I3rabRuleSeedRow("SUFFIX:PRON:2MS", "PRON.SUF", "ضمير متصل للمخاطب", I3rabStatusMapping.Approved, null, 3)
        ]);

        var assembler = new I3rabAssembler(catalog);
        var inputs = new List<I3rabSegmentInput>
        {
            new(10, 100, 1, "PREFIX", "P", "PREFIX", null, null, null, false, false),
            new(11, 100, 2, "STEM", "N", "GEN", "genitive", null, null, false, false),
            new(12, 100, 3, "SUFFIX", "PRON", "2MS", null, null, null, false, false)
        };

        var labels = assembler.Assemble(inputs);

        labels.Should().HaveCount(3);
        labels.Should().AllSatisfy(label =>
        {
            label.Status.Should().Be(I3rabStatusMapping.Approved);
            label.I3rabArabic.Should().NotBeNullOrWhiteSpace();
            label.ReviewReason.Should().BeNull();
        });

        labels[0].SignatureKey.Should().Be("PREFIX:P");
        labels[0].I3rabArabic.Should().Be("حرف جر");
        labels[1].SignatureKey.Should().Be("STEM:N:GEN");
        labels[1].I3rabArabic.Should().Be("اسم مجرور");
        labels[2].SignatureKey.Should().Be("SUFFIX:PRON:2MS");
        labels[2].I3rabArabic.Should().Be("ضمير متصل للمخاطب");
    }

    private sealed class InMemoryCatalog(IReadOnlyList<I3rabRuleSeedRow> rows) : II3rabRuleCatalog
    {
        private readonly Dictionary<string, I3rabRuleSeedRow> rowsBySignature =
            rows.ToDictionary(row => row.SignatureKey, StringComparer.Ordinal);

        public IReadOnlyList<I3rabRuleSeedRow> Rows() => rows;

        public bool TryGet(string signatureKey, out I3rabRuleSeedRow row) =>
            rowsBySignature.TryGetValue(signatureKey, out row!);
    }
}
