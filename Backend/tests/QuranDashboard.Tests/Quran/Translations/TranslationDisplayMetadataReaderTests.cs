using System.Text.Json.Nodes;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationDisplayMetadataReaderTests : IDisposable
{
    private readonly TranslationDisplayMetadataReader reader = new();
    private readonly TranslationSyntheticPackage packages = new();

    public void Dispose() => packages.Dispose();

    [Fact]
    public async Task ReadAsync_parses_final_display_metadata_contract()
    {
        var packageDir = await packages.WriteAsync();
        var approvedKeys = new[] { "en-test-simple" };

        var metadata = await reader.ReadAsync(
            packageDir,
            approvedKeys,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        metadata.MetadataType.Should().Be(TranslationImportConstants.DisplayMetadataType);
        metadata.Status.Should().Be(TranslationImportConstants.DisplayMetadataFinalStatus);
        metadata.SourceCount.Should().Be(1);

        var record = metadata.Records.Single();
        record.SourceKey.Should().Be("en-test-simple");
        record.MetadataStatus.Should().Be(TranslationImportConstants.DisplayRecordFinalStatus);
        record.DisplayNameEn.Should().NotBeNullOrWhiteSpace();
        record.DisplayNameAr.Should().NotBeNullOrWhiteSpace();
        record.LanguageCode.Should().Be("en");
        record.PackageFile.Should().Be("sources/en-test-simple.json");
    }

    [Fact]
    public async Task ReadAsync_requires_manifest_source_set_alignment()
    {
        var packageDir = await packages.WriteAsync(
            sources: TranslationSyntheticSeed.IntegrationSources);
        var approvedKeys = TranslationSyntheticSeed.IntegrationSources
            .Select(source => source.SourceKey)
            .ToArray();

        var metadata = await reader.ReadAsync(
            packageDir,
            approvedKeys,
            TranslationSyntheticSeed.IntegrationTestExpectedCounts,
            CancellationToken.None);

        metadata.Records.Select(record => record.SourceKey)
            .Should()
            .BeEquivalentTo(approvedKeys);
    }

    [Fact]
    public async Task ReadAsync_refuses_source_set_mismatch_with_manifest()
    {
        var packageDir = await packages.WriteAsync();
        var approvedKeys = new[] { "en-test-simple", "missing-source-key" };

        var act = () => reader.ReadAsync(
            packageDir,
            approvedKeys,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        await act.Should().ThrowAsync<TranslationValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TranslationInvariants.CheckDisplayMetadataSet));
    }

    [Fact]
    public async Task ReadAsync_throws_when_display_metadata_file_is_missing()
    {
        var packageDir = await packages.WriteAsync();
        File.Delete(Path.Combine(packageDir, "source-display-metadata.json"));

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        await act.Should().ThrowAsync<TranslationSourceException>();
    }

    [Fact]
    public async Task ReadAsync_throws_on_invalid_json()
    {
        var packageDir = await packages.WriteAsync();
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "source-display-metadata.json"),
            "{ this is not valid json");

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadAsync_fails_tr_display_metadata_final_when_status_is_not_final()
    {
        var packageDir = await packages.WriteAsync();
        await ModifyDisplayMetadataAsync(packageDir, root =>
        {
            root["status"] = "draft";
        });

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckDisplayMetadataFinal && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_tr_display_metadata_final_when_source_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync();
        await ModifyDisplayMetadataAsync(packageDir, root =>
        {
            root["sourceCount"] = 999;
        });

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckDisplayMetadataFinal && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_tr_display_metadata_required_fields_when_display_name_is_empty()
    {
        var packageDir = await packages.WriteAsync();
        await ModifyDisplayMetadataAsync(packageDir, root =>
        {
            root["records"]![0]!["displayNameEn"] = "";
        });

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckDisplayMetadataRequiredFields && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_tr_display_metadata_required_fields_when_record_status_is_not_final()
    {
        var packageDir = await packages.WriteAsync();
        await ModifyDisplayMetadataAsync(packageDir, root =>
        {
            root["records"]![0]!["metadataStatus"] = "needs_review";
        });

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckDisplayMetadataRequiredFields && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_fails_tr_display_metadata_required_fields_when_translation_type_is_invalid()
    {
        var packageDir = await packages.WriteAsync();
        await ModifyDisplayMetadataAsync(packageDir, root =>
        {
            root["records"]![0]!["translationType"] = "word_by_word";
        });

        var act = () => reader.ReadAsync(
            packageDir,
            ["en-test-simple"],
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckDisplayMetadataRequiredFields
                && !check.Passed
                && check.Observed.Contains("en-test-simple:translationType", StringComparison.Ordinal));
    }

    private static async Task ModifyDisplayMetadataAsync(string packageDir, Action<JsonObject> modify)
    {
        var path = Path.Combine(packageDir, "source-display-metadata.json");
        var json = await File.ReadAllTextAsync(path);
        var root = JsonNode.Parse(json)!.AsObject();
        modify(root);
        await File.WriteAllTextAsync(path, root.ToJsonString());
    }
}
