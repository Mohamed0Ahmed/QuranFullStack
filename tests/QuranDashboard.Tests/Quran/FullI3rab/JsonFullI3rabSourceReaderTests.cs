using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class JsonFullI3rabSourceReaderTests : IDisposable
{
    private readonly JsonFullI3rabSourceReader reader = new();
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task ReadAsync_parses_text_owning_values_string_pointers_and_inline_markup()
    {
        var packageDir = await packages.WriteAsync();
        var sourcePath = packages.GetSourceFilePath(packageDir, "test-muyassar");

        var parsed = await reader.ReadAsync(sourcePath, CancellationToken.None);

        parsed.Entries.Should().HaveCount(3);
        parsed.Entries["900:1"].Should().BeOfType<ParsedFullI3rabSourceEntry.TextOwning>();
        parsed.Entries["900:2"].Should().BeOfType<ParsedFullI3rabSourceEntry.Pointer>();
        parsed.Entries["900:3"].Should().BeOfType<ParsedFullI3rabSourceEntry.TextOwning>();

        var leader = (ParsedFullI3rabSourceEntry.TextOwning)parsed.Entries["900:1"];
        leader.I3rabHtml.Should().Contain("<p>");
        leader.I3rabHtml.Should().Contain("900:1");
        leader.AyahKeys.Should().BeEquivalentTo(["900:1", "900:2"]);

        var pointer = (ParsedFullI3rabSourceEntry.Pointer)parsed.Entries["900:2"];
        pointer.LeaderVerseKey.Should().Be("900:1");
    }

    [Fact]
    public async Task ReadAsync_preserves_raw_html_byte_for_byte()
    {
        var rawHtml = "<div class=\"ar\"><span class=\"hlt qpc-hafs\">﴿1﴾</span></div>";
        var path = await WriteTempSourceAsync(new Dictionary<string, object>
        {
            ["900:1"] = new { text = rawHtml }
        });

        var parsed = await reader.ReadAsync(path, CancellationToken.None);

        var leader = (ParsedFullI3rabSourceEntry.TextOwning)parsed.Entries["900:1"];
        leader.I3rabHtml.Should().Be(rawHtml);
    }

    [Fact]
    public async Task ReadAsync_refuses_empty_text()
    {
        var path = await WriteTempSourceAsync(new Dictionary<string, object>
        {
            ["900:1"] = new { text = "" }
        });

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabSourceException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task ReadAsync_refuses_malformed_json_root()
    {
        var path = Path.Combine(Path.GetTempPath(), $"full-i3rab-malformed-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, "[1, 2, 3]");
        try
        {
            var act = () => reader.ReadAsync(path, CancellationToken.None);
            await act.Should().ThrowAsync<FullI3rabSourceException>()
                .WithMessage("*JSON object*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> WriteTempSourceAsync(IReadOnlyDictionary<string, object> entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"full-i3rab-source-{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    public void Dispose() => packages.Dispose();
}
