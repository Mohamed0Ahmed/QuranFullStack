using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

public sealed class TafsirSourceReaderTests
{
    private readonly JsonTafsirSourceReader reader = new();
    private readonly TafsirImportTestFixture fixture = new();

    [Fact]
    public async Task ReadAsync_parses_object_values_string_pointers_and_inline_markup()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");

        var parsed = await reader.ReadAsync(sourcePath, CancellationToken.None);

        parsed.Entries.Should().HaveCount(3);
        parsed.Entries["900:1"].Should().BeOfType<ParsedTafsirSourceEntry.TextOwning>();
        parsed.Entries["900:2"].Should().BeOfType<ParsedTafsirSourceEntry.Pointer>();
        parsed.Entries["900:3"].Should().BeOfType<ParsedTafsirSourceEntry.TextOwning>();

        var leader = (ParsedTafsirSourceEntry.TextOwning)parsed.Entries["900:1"];
        leader.Text.Should().Contain("<p>");
        leader.Text.Should().Contain("900:1");
        leader.AyahKeys.Should().BeEquivalentTo(["900:1", "900:2"]);

        var pointer = (ParsedTafsirSourceEntry.Pointer)parsed.Entries["900:2"];
        pointer.LeaderVerseKey.Should().Be("900:1");
    }

    [Fact]
    public async Task ReadAsync_refuses_empty_text()
    {
        var path = await WriteTempSourceAsync(new Dictionary<string, object>
        {
            ["900:1"] = new { text = "" }
        });

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirSourceException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task ReadAsync_refuses_malformed_json_root()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tafsir-malformed-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, "[1, 2, 3]");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirSourceException>()
            .WithMessage("*JSON object*");
    }

    private static async Task<string> WriteTempSourceAsync(IReadOnlyDictionary<string, object> entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tafsir-source-{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
