using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Infrastructure.Files.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationSourceReaderTests
{
    private readonly JsonTranslationSourceReader reader = new();

    private static string WriteSourceFile(string jsonContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"translation-src-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "source.json");
        File.WriteAllText(path, jsonContent);
        return path;
    }

    [Fact]
    public async Task ReadAsync_parses_object_root_with_t_values_and_preserves_exact_text()
    {
        var path = WriteSourceFile(
            """
            {
              "901:1": { "t": "SYNTHETIC-TRANSLATION-en-test-minimal-901:1" },
              "901:2": { "t": "SYNTHETIC-TRANSLATION-en-test-minimal-901:2" },
              "901:3": { "t": "SYNTHETIC-TRANSLATION-en-test-minimal-901:3" }
            }
            """);

        var parsed = await reader.ReadAsync(path, CancellationToken.None);

        parsed.Entries.Should().HaveCount(3);
        parsed.Entries["901:1"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:1");
        parsed.Entries["901:2"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:2");
        parsed.Entries["901:3"].Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:3");
    }

    [Fact]
    public async Task ReadAsync_preserves_whitespace_and_markup_exactly()
    {
        const string exactText = "  SYNTHETIC <b>markup</b> [[note]]  ";
        var path = WriteSourceFile(
            JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["901:1"] = new { t = exactText }
            }));

        var parsed = await reader.ReadAsync(path, CancellationToken.None);

        parsed.Entries["901:1"].Should().Be(exactText);
    }

    [Fact]
    public async Task ReadAsync_fails_on_array_root_shape_with_tr_json_shape()
    {
        var path = WriteSourceFile("""[{"901:1":{"t":"text"}}]""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckJsonShape && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_missing_t_property_with_tr_no_empty_text()
    {
        var path = WriteSourceFile("""{"901:1":{}}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoEmptyText && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_null_t_with_tr_no_empty_text()
    {
        var path = WriteSourceFile("""{"901:1":{"t":null}}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoEmptyText && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_non_string_t_with_tr_no_empty_text()
    {
        var path = WriteSourceFile("""{"901:1":{"t":123}}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoEmptyText && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_empty_t_with_tr_no_empty_text()
    {
        var path = WriteSourceFile("""{"901:1":{"t":""}}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoEmptyText && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_non_object_value_with_tr_json_shape()
    {
        var path = WriteSourceFile("""{"901:1":"plain-string"}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckJsonShape && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_on_malformed_verse_key_with_tr_ayah_keys_resolve()
    {
        var path = WriteSourceFile("""{"not-a-verse-key":{"t":"text"}}""");

        var act = () => reader.ReadAsync(path, CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckAyahKeysResolve && !check.Passed);
    }
}
