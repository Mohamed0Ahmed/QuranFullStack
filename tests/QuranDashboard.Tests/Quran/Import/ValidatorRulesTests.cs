using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Application.Quran.Import.Validation;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class ValidatorRulesTests
{
    private readonly QuranImportValidator validator = new();

    [Fact]
    public void Validate_IncludesEveryFr018CheckWithCanonicalSeverity()
    {
        var source = new QuranImportSourceData(
            Surahs: [],
            Ayahs: [],
            Glyph: [],
            Uthmani: [],
            UthmaniSimple: [],
            ImlaeiSimple: [],
            Layout: new LayoutDto(0, 0, new Dictionary<int, IReadOnlyList<LineDto>>()),
            ManifestVersion: "1");

        var assembled = new AssembledQuranData([], [], [], [], []);

        var result = validator.Validate(
            "/tmp/quran-source",
            "1",
            source,
            assembled,
            forced: false);

        result.Checks.Select(check => check.Id).Should().BeEquivalentTo(ImportValidationCheckIds.All);

        foreach (var check in result.Checks)
        {
            ImportValidationCheckIds.Severities[check.Id].Should().Be(check.Severity);
        }
    }
}
