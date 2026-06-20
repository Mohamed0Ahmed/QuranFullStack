using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationValidationFailureTests(NavigationImportTestFixture fixture)
{
    private readonly NavigationMetadataAssembler assembler = new();

    [Fact]
    public async Task Juz_gap_fails_with_nav_range_coverage_juz_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.JuzGapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckRangeCoverageJuz);

        File.Exists(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Rub_overlap_fails_with_nav_no_range_gaps_overlaps_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.RubOverlapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckNoRangeGapsOverlaps);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Unresolved_sajda_verse_key_fails_with_nav_verse_keys_resolve_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.UnresolvedSajdaPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckVerseKeysResolve);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Invalid_sajda_type_fails_with_nav_sajda_type_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.InvalidSajdaTypePackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckSajdaType);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Hizb_spanning_two_juz_fails_with_nav_hierarchy_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var expectedCounts = NavigationSyntheticSeed.DefaultTestExpectedCounts with { Hizb = 1 };
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            expectedCounts,
            NavigationSyntheticSeed.HizbSpanningTwoJuzPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(packageDir, expectedCounts, reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckHierarchy);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Empty_quran_ayahs_refuses_with_ayahs_missing_and_writes_reports()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.TruncateFoundationAsync();

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.AyahsMissing);
        result.ExitCode.Should().Be(ImportNavigationMetadataResult.RefusedExitCode);

        File.Exists(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Non_contiguous_juz_numbers_fails_with_nav_json_shape_and_writes_nothing()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.NonContiguousJuzNumbersPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckJsonShape);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }

    [Fact]
    public void Invalid_sajda_type_in_source_dto_fails_with_nav_sajda_type()
    {
        var runner = new NavigationMetadataValidationRunner(new NavigationMetadataAssembler());
        var ayahIds = NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

        var source = BuildSourceFromSpec(NavigationSyntheticSeed.InvalidSajdaTypePackageSpec);

        var act = () => runner.AssembleAndValidate(
            source,
            ayahIds,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        act.Should().Throw<NavigationMetadataValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckSajdaType && !check.Passed);
    }

    [Fact]
    public void Non_contiguous_juz_numbers_in_source_dto_fails_with_nav_json_shape()
    {
        var runner = new NavigationMetadataValidationRunner(new NavigationMetadataAssembler());
        var ayahIds = NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

        var act = () => runner.AssembleAndValidate(
            BuildSourceFromSpec(NavigationSyntheticSeed.NonContiguousJuzNumbersPackageSpec),
            ayahIds,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        act.Should().Throw<NavigationMetadataValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckJsonShape && !check.Passed);
    }

    [Fact]
    public void Unresolved_verse_key_fails_with_nav_verse_keys_resolve()
    {
        var ayahIds = NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

        var source = BuildSourceWithSajdaVerseKey(NavigationSyntheticSeed.SyntheticVerseKey(99));

        var act = () => assembler.Assemble(
            source,
            ayahIds,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        act.Should().Throw<NavigationMetadataValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckVerseKeysResolve && !check.Passed);
    }

    [Fact]
    public void Incomplete_juz_coverage_fails_with_nav_range_coverage_juz()
    {
        var ayahIds = NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

        var act = () => assembler.Assemble(
            BuildSourceFromSpec(NavigationSyntheticSeed.JuzGapPackageSpec),
            ayahIds,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        act.Should().Throw<NavigationMetadataValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckRangeCoverageJuz && !check.Passed);
    }

    private static NavigationMetadataSourceData BuildSourceWithSajdaVerseKey(string verseKey) =>
        BuildSourceFromSpec(NavigationSyntheticSeed.DefaultPackageSpec with
        {
            Sajda =
            [
                new SyntheticNavigationSajdaSpec(1, verseKey, "optional"),
                new SyntheticNavigationSajdaSpec(2, NavigationSyntheticSeed.SyntheticVerseKey(5), "required")
            ]
        });

    private static NavigationMetadataSourceData BuildSourceFromSpec(SyntheticNavigationPackageSpec spec) =>
        new(
            spec.Juz.Select(division => new NavigationDivisionDto(
                division.Number,
                division.VersesCount,
                division.FirstVerseKey,
                division.LastVerseKey,
                division.VerseMapping)).ToList(),
            spec.Hizb.Select(division => new NavigationDivisionDto(
                division.Number,
                division.VersesCount,
                division.FirstVerseKey,
                division.LastVerseKey,
                division.VerseMapping)).ToList(),
            spec.Rub.Select(division => new NavigationDivisionDto(
                division.Number,
                division.VersesCount,
                division.FirstVerseKey,
                division.LastVerseKey,
                division.VerseMapping)).ToList(),
            spec.Sajda.Select(sajda => new NavigationSajdaDto(
                sajda.SajdahNumber,
                sajda.VerseKey,
                sajda.SajdahType)).ToList(),
            []);
}
