using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationValidationRuleTests
{
    private readonly NavigationMetadataAssembler assembler = new();

    [Fact]
    public void Invalid_sajda_type_in_source_dto_fails_with_nav_sajda_type()
    {
        var runner = new NavigationMetadataValidationRunner(new NavigationMetadataAssembler());
        var ayahIds = BuildAyahIdLookup();

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
        var ayahIds = BuildAyahIdLookup();

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
        var ayahIds = BuildAyahIdLookup();

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
        var ayahIds = BuildAyahIdLookup();

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

    private static IReadOnlyDictionary<string, int> BuildAyahIdLookup() =>
        NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

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
