using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class FullI3rabValidationFailureTests : IDisposable
{
    private readonly FullI3rabManifestReader manifestReader = new();
    private readonly FullI3rabAssembler assembler = new();
    private readonly JsonFullI3rabSourceReader sourceReader = new();
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task ReadAsync_fails_PACKAGE_SHAPE_when_required_root_file_missing()
    {
        var packageDir = await packages.WriteAsync();
        File.Delete(Path.Combine(packageDir, "README.md"));

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckPackageShape));
    }

    [Theory]
    [InlineData("draft-full-i3rab-manifest")]
    [InlineData("quran-tafsir-import-source-package")]
    public async Task ReadAsync_fails_MANIFEST_FINAL_for_wrong_manifest_type(string wrongType)
    {
        var packageDir = await packages.WriteAsync(manifestType: wrongType);

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_MANIFEST_FINAL_when_not_final_import_manifest()
    {
        var packageDir = await packages.WriteAsync(isFinalImportManifest: false);

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_SOURCE_COUNT_when_declared_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync(sourceCountOverride: 99);

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_COVERAGE_COUNT_when_canonical_coverage_is_wrong()
    {
        var packageDir = await packages.WriteAsync(coverageOverride: 10);

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckCoverageCount));
    }

    [Fact]
    public async Task ReadAsync_fails_SOURCE_HASH_when_file_size_changes()
    {
        var packageDir = await packages.WriteAsync();
        await File.AppendAllTextAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            " ");

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckSourceHash));
    }

    [Fact]
    public async Task ReadAsync_fails_SOURCE_SET_when_extra_unapproved_file_is_present()
    {
        var packageDir = await packages.WriteAsync();
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "sources", "unapproved-extra.json"),
            "{}");

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckSourceSet));
    }

    [Fact]
    public async Task Assemble_fails_JSON_SHAPE_when_key_count_mismatches_expected()
    {
        var (manifest, parsed) = await ReadDefaultPackageAsync();

        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                ["test-muyassar"] = parsed
            },
            ayahIds,
            expectedAyahsPerSource: 4);

        act.Should().Throw<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckJsonShape));
    }

    [Fact]
    public async Task Assemble_fails_AYAH_KEYS_RESOLVE_when_verse_key_is_unknown()
    {
        var (manifest, parsed) = await ReadDefaultPackageAsync();
        var ayahIds = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["900:2"] = 2,
            ["900:3"] = 3
        };

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                ["test-muyassar"] = parsed
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        act.Should().Throw<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckAyahKeysResolve));
    }

    [Fact]
    public async Task Assemble_fails_BLOCK_PARTITION_when_two_leaders_claim_the_same_ayah()
    {
        var spec = new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-overlap",
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:1"), ayah_keys = new[] { "900:1", "900:2" } },
                ["900:2"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:2"), ayah_keys = new[] { "900:2" } },
                ["900:3"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:3") }
            },
            ObservedHtmlTags: new Dictionary<string, int>(),
            ObservedHtmlClasses: new Dictionary<string, int>());

        var (manifest, parsed) = await ReadCustomPackageAsync(spec);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                [spec.SourceKey] = parsed
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        act.Should().Throw<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckBlockPartition));
    }

    [Fact]
    public async Task Assemble_fails_AYAH_KEYS_MEMBER_MATCH_when_pointer_target_does_not_cover_member()
    {
        var spec = new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-member-mismatch",
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:1"), ayah_keys = new[] { "900:1", "900:3" } },
                ["900:2"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:2"), ayah_keys = new[] { "900:2" } },
                ["900:3"] = "900:2"
            },
            ObservedHtmlTags: new Dictionary<string, int>(),
            ObservedHtmlClasses: new Dictionary<string, int>());

        var (manifest, parsed) = await ReadCustomPackageAsync(spec);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                [spec.SourceKey] = parsed
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        act.Should().Throw<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckAyahKeysMemberMatch));
    }

    [Fact]
    public async Task Assemble_fails_POINTERS_RESOLVE_when_pointer_targets_a_non_text_owning_entry()
    {
        var spec = new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-pointer-resolve",
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = FullI3rabSyntheticSeed.SyntheticHtml("900:1"), ayah_keys = new[] { "900:1", "900:2", "900:3" } },
                ["900:2"] = "900:3",
                ["900:3"] = "900:1"
            },
            ObservedHtmlTags: new Dictionary<string, int>(),
            ObservedHtmlClasses: new Dictionary<string, int>());

        var (manifest, parsed) = await ReadCustomPackageAsync(spec);
        var ayahIds = FullI3rabSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey, pair => pair.Id, StringComparer.Ordinal);

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedFullI3rabSourceFile>(StringComparer.Ordinal)
            {
                [spec.SourceKey] = parsed
            },
            ayahIds,
            expectedAyahsPerSource: 3);

        act.Should().Throw<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckPointersResolve));
    }

    private async Task<(FullI3rabPackageManifest Manifest, ParsedFullI3rabSourceFile Parsed)> ReadDefaultPackageAsync()
    {
        var packageDir = await packages.WriteAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            CancellationToken.None);
        return (manifest, parsed);
    }

    private async Task<(FullI3rabPackageManifest Manifest, ParsedFullI3rabSourceFile Parsed)> ReadCustomPackageAsync(
        SyntheticFullI3rabSourceSpec spec)
    {
        var packageDir = await packages.WriteAsync(sources: [spec]);
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(
            packages.GetSourceFilePath(packageDir, spec.SourceKey),
            CancellationToken.None);
        return (manifest, parsed);
    }

    public void Dispose() => packages.Dispose();
}
