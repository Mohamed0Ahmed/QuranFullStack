using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Infrastructure.Files.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationSourceReaderTests
{
    private readonly JsonTranslationSourceReader reader = new();
    private readonly TranslationImportTestFixture fixture = new();

    [Fact]
    public async Task ReadAsync_parses_object_root_with_t_values_and_preserves_exact_text()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.MinimalSources);
        var sourcePath = Path.Combine(packageDir, "sources", "en-test-minimal.json");

        var parsed = await reader.ReadAsync(sourcePath, CancellationToken.None);

        parsed.Entries.Should().HaveCount(3);
        parsed.Entries["901:1"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:1");
        parsed.Entries["901:2"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:2");
        parsed.Entries["901:3"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:3");
    }

    [Fact]
    public async Task ReadAsync_handles_full_6236_key_set()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "en-test-simple.json");

        var parsed = await reader.ReadAsync(sourcePath, CancellationToken.None);

        parsed.Entries.Should().HaveCount(TranslationInvariants.ExpectedAyahsPerSource);
        parsed.Entries.Should().ContainKey("901:1");
        parsed.Entries.Should().ContainKey($"901:{TranslationInvariants.ExpectedAyahsPerSource}");
    }

    [Fact]
    public async Task ReadAsync_preserves_whitespace_and_markup_exactly()
    {
        var packageDir = Path.Combine(Path.GetTempPath(), $"translation-source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(Path.Combine(packageDir, "sources"));

        const string exactText = "  SYNTHETIC <b>markup</b> [[note]]  ";
        var sourcePath = Path.Combine(packageDir, "sources", "en-exact.json");
        await File.WriteAllTextAsync(
            sourcePath,
            JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["901:1"] = new { t = exactText }
            }));

        var parsed = await reader.ReadAsync(sourcePath, CancellationToken.None);

        parsed.Entries["901:1"].Should().Be(exactText);
    }
}
