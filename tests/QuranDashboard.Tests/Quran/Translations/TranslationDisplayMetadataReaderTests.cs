using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Infrastructure.Files.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationDisplayMetadataReaderTests
{
    private readonly TranslationDisplayMetadataReader reader = new();
    private readonly TranslationImportTestFixture fixture = new();

    [Fact]
    public async Task ReadAsync_parses_final_display_metadata_contract()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
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
        var packageDir = await fixture.WriteSyntheticPackageAsync(
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
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var approvedKeys = new[] { "en-test-simple", "missing-source-key" };

        var act = () => reader.ReadAsync(
            packageDir,
            approvedKeys,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        await act.Should().ThrowAsync<TranslationValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TranslationInvariants.CheckDisplayMetadataSet));
    }
}
