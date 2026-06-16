using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Application.Quran.Navigation.ImportNavigationMetadata;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

/// <summary>
/// Shared test fixture for Feature 009 (Quran Navigation Metadata Foundation).
/// Uses synthetic surah 901 and clearly synthetic navigation metadata only.
/// </summary>
public sealed class NavigationImportTestFixture : IAsyncLifetime
{
    private const int SyntheticSurahNumber = NavigationSyntheticSeed.SyntheticSurahNumber;

    private readonly List<string> tempDirs = new();

    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var dir in tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        await postgresContainer.DisposeAsync();
    }

    public ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration);

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public async Task SeedSyntheticAyahsAsync(params (int Id, string VerseKey)[] ayahs)
    {
        await TruncateFoundationAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.QuranSurahs.Add(new Surah
        {
            SurahNumber = SyntheticSurahNumber,
            NameArabic = "سورة-اختبار-٩٠١",
            NameSimple = "TEST_SURAH_901",
            NameTransliteration = "TEST_SURAH_901",
            RevelationPlace = RevelationPlace.Makkah,
            RevelationOrder = 901,
            VersesCount = (short)ayahs.Length,
            BismillahPre = false
        });

        dbContext.QuranMushafPages.Add(new MushafPage
        {
            PageNumber = 901,
            FirstSurahNumber = SyntheticSurahNumber,
            FirstAyahNumber = 1,
            LastSurahNumber = SyntheticSurahNumber,
            LastAyahNumber = (short)ayahs.Length,
            LinesCount = 15
        });

        await dbContext.SaveChangesAsync();

        foreach (var (id, verseKey) in ayahs)
        {
            var parts = verseKey.Split(':');
            dbContext.QuranAyahs.Add(new Ayah
            {
                Id = id,
                SurahNumber = short.Parse(parts[0]),
                AyahNumber = short.Parse(parts[1]),
                VerseKey = verseKey,
                TextUthmani = $"اختبار-{verseKey}",
                WordsCountSource = 5,
                WordsCountReal = 5,
                PageFrom = 901,
                PageTo = 901
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task TruncateNavigationTablesAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE quran_sajdas, quran_rubs, quran_hizbs, quran_juzs RESTART IDENTITY CASCADE;
            UPDATE quran_ayahs SET juz_number = NULL, hizb_number = NULL, rub_number = NULL;
            """);
    }

    public async Task TruncateFoundationAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE
                quran_ayahs,
                quran_mushaf_pages,
                quran_surahs
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task<string> WriteSyntheticPackageAsync(
        NavigationExpectedCounts expectedCounts,
        SyntheticNavigationPackageSpec? spec = null) =>
        await NavigationSyntheticPackageWriter.WritePackageAsync(
            expectedCounts,
            spec,
            tempDirs.Add);

    public async Task<ImportNavigationMetadataResult> RunImportAsync(
        string packageDir,
        NavigationExpectedCounts expectedCounts,
        string? reportOutDir = null,
        bool force = false)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportNavigationMetadataHandler>();
        return await handler.HandleAsync(
            new ImportNavigationMetadataCommand(packageDir, force, expectedCounts, reportOutDir),
            CancellationToken.None);
    }

    public async Task<ImportNavigationMetadataResult> RunImportWithTamperAfterLoadAsync(
        string packageDir,
        NavigationExpectedCounts expectedCounts,
        Action<string> tamper,
        string? reportOutDir = null)
    {
        reportOutDir ??= Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        await using var scope = CreateServiceProvider(services =>
        {
            services.AddScoped<INavigationMetadataImportSource>(sp =>
                new TamperingNavigationImportSource(
                    new NavigationMetadataImportSource(
                        sp.GetRequiredService<NavigationManifestReader>(),
                        sp.GetRequiredService<JsonNavigationDatasetReader>()),
                    tamper));
        }).CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ImportNavigationMetadataHandler>();
        return await handler.HandleAsync(
            new ImportNavigationMetadataCommand(packageDir, Force: false, expectedCounts, reportOutDir),
            CancellationToken.None);
    }

    public async Task<NavigationTableSnapshot> CaptureNavigationSnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new NavigationTableSnapshot(
            await dbContext.QuranJuzs.CountAsync(),
            await dbContext.QuranHizbs.CountAsync(),
            await dbContext.QuranRubs.CountAsync(),
            await dbContext.QuranSajdas.CountAsync(),
            await dbContext.QuranAyahs.CountAsync(ayah =>
                ayah.JuzNumber != null || ayah.HizbNumber != null || ayah.RubNumber != null));
    }
}

public sealed record NavigationTableSnapshot(
    int JuzRows,
    int HizbRows,
    int RubRows,
    int SajdaRows,
    int TaggedAyahRows);

internal sealed class TamperingNavigationImportSource : INavigationMetadataImportSource
{
    private readonly INavigationMetadataImportSource inner;
    private readonly Action<string> tamperAfterLoad;

    public TamperingNavigationImportSource(
        INavigationMetadataImportSource inner,
        Action<string> tamperAfterLoad)
    {
        this.inner = inner;
        this.tamperAfterLoad = tamperAfterLoad;
    }

    public async Task<NavigationMetadataSourceData> LoadAsync(
        string sourcePath,
        NavigationExpectedCounts expected,
        CancellationToken ct)
    {
        var source = await inner.LoadAsync(sourcePath, expected, ct);
        tamperAfterLoad(sourcePath);
        return source;
    }

    public Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct) =>
        inner.SourceUnchangedAsync(sourcePath, ct);
}

public sealed record SyntheticNavigationPackageSpec(
    IReadOnlyList<SyntheticNavigationDivisionSpec> Juz,
    IReadOnlyList<SyntheticNavigationDivisionSpec> Hizb,
    IReadOnlyList<SyntheticNavigationDivisionSpec> Rub,
    IReadOnlyList<SyntheticNavigationSajdaSpec> Sajda);

public sealed record SyntheticNavigationDivisionSpec(
    short Number,
    short VersesCount,
    string FirstVerseKey,
    string LastVerseKey,
    IReadOnlyDictionary<string, string> VerseMapping);

public sealed record SyntheticNavigationSajdaSpec(
    short SajdahNumber,
    string VerseKey,
    string SajdahType);

internal static class NavigationSyntheticSeed
{
    public const int SyntheticSurahNumber = 901;

    public static string SyntheticVerseKey(int ayahNumber) =>
        $"{SyntheticSurahNumber}:{ayahNumber}";

    public static (int Id, string VerseKey)[] DefaultAyahs =>
    [
        (1, SyntheticVerseKey(1)),
        (2, SyntheticVerseKey(2)),
        (3, SyntheticVerseKey(3)),
        (4, SyntheticVerseKey(4)),
        (5, SyntheticVerseKey(5)),
        (6, SyntheticVerseKey(6))
    ];

    public static NavigationExpectedCounts DefaultTestExpectedCounts =>
        new(Juz: 2, Hizb: 2, Rub: 4, Sajda: 2, Ayahs: 6);

    public static SyntheticNavigationPackageSpec DefaultPackageSpec => new(
        Juz:
        [
            new SyntheticNavigationDivisionSpec(1, 3, SyntheticVerseKey(1), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "1-3" }),
            new SyntheticNavigationDivisionSpec(2, 3, SyntheticVerseKey(4), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "4-6" })
        ],
        Hizb:
        [
            new SyntheticNavigationDivisionSpec(1, 3, SyntheticVerseKey(1), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "1-3" }),
            new SyntheticNavigationDivisionSpec(2, 3, SyntheticVerseKey(4), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "4-6" })
        ],
        Rub:
        [
            new SyntheticNavigationDivisionSpec(1, 2, SyntheticVerseKey(1), SyntheticVerseKey(2), new Dictionary<string, string> { ["901"] = "1-2" }),
            new SyntheticNavigationDivisionSpec(2, 1, SyntheticVerseKey(3), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "3-3" }),
            new SyntheticNavigationDivisionSpec(3, 2, SyntheticVerseKey(4), SyntheticVerseKey(5), new Dictionary<string, string> { ["901"] = "4-5" }),
            new SyntheticNavigationDivisionSpec(4, 1, SyntheticVerseKey(6), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "6-6" })
        ],
        Sajda:
        [
            new SyntheticNavigationSajdaSpec(1, SyntheticVerseKey(2), "optional"),
            new SyntheticNavigationSajdaSpec(2, SyntheticVerseKey(5), "required")
        ]);

    public static readonly (int AyahId, short JuzNumber, short HizbNumber, short RubNumber)[] ExpectedAyahAssignments =
    [
        (1, 1, 1, 1),
        (2, 1, 1, 1),
        (3, 1, 1, 2),
        (4, 2, 2, 3),
        (5, 2, 2, 3),
        (6, 2, 2, 4)
    ];

    public static readonly (short SajdahNumber, string VerseKey, string SajdahType)[] ExpectedSajdaRows =
    [
        (1, SyntheticVerseKey(2), "optional"),
        (2, SyntheticVerseKey(5), "required")
    ];

    public static SyntheticNavigationPackageSpec JuzGapPackageSpec => new(
        Juz:
        [
            new SyntheticNavigationDivisionSpec(1, 2, SyntheticVerseKey(1), SyntheticVerseKey(2), new Dictionary<string, string> { ["901"] = "1-2" }),
            new SyntheticNavigationDivisionSpec(2, 3, SyntheticVerseKey(4), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "4-6" })
        ],
        Hizb: DefaultPackageSpec.Hizb,
        Rub: DefaultPackageSpec.Rub,
        Sajda: DefaultPackageSpec.Sajda);

    public static SyntheticNavigationPackageSpec RubOverlapPackageSpec => new(
        Juz: DefaultPackageSpec.Juz,
        Hizb: DefaultPackageSpec.Hizb,
        Rub:
        [
            new SyntheticNavigationDivisionSpec(1, 3, SyntheticVerseKey(1), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "1-3" }),
            new SyntheticNavigationDivisionSpec(2, 2, SyntheticVerseKey(3), SyntheticVerseKey(4), new Dictionary<string, string> { ["901"] = "3-4" }),
            new SyntheticNavigationDivisionSpec(3, 1, SyntheticVerseKey(5), SyntheticVerseKey(5), new Dictionary<string, string> { ["901"] = "5-5" }),
            new SyntheticNavigationDivisionSpec(4, 1, SyntheticVerseKey(6), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "6-6" })
        ],
        Sajda: DefaultPackageSpec.Sajda);

    public static SyntheticNavigationPackageSpec HizbSpanningTwoJuzPackageSpec => new(
        Juz: DefaultPackageSpec.Juz,
        Hizb:
        [
            new SyntheticNavigationDivisionSpec(1, 6, SyntheticVerseKey(1), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "1-6" })
        ],
        Rub: DefaultPackageSpec.Rub,
        Sajda: DefaultPackageSpec.Sajda);

    public static SyntheticNavigationPackageSpec UnresolvedSajdaPackageSpec => DefaultPackageSpec with
    {
        Sajda =
        [
            new SyntheticNavigationSajdaSpec(1, SyntheticVerseKey(99), "optional"),
            new SyntheticNavigationSajdaSpec(2, SyntheticVerseKey(5), "required")
        ]
    };

    public static SyntheticNavigationPackageSpec InvalidSajdaTypePackageSpec => DefaultPackageSpec with
    {
        Sajda =
        [
            new SyntheticNavigationSajdaSpec(1, SyntheticVerseKey(2), "mandatory"),
            new SyntheticNavigationSajdaSpec(2, SyntheticVerseKey(5), "required")
        ]
    };

    public static SyntheticNavigationPackageSpec NonContiguousJuzNumbersPackageSpec => DefaultPackageSpec with
    {
        Juz =
        [
            new SyntheticNavigationDivisionSpec(1, 3, SyntheticVerseKey(1), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "1-3" }),
            new SyntheticNavigationDivisionSpec(3, 3, SyntheticVerseKey(4), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "4-6" })
        ]
    };
}

[CollectionDefinition(nameof(NavigationImportTestCollection))]
public sealed class NavigationImportTestCollection : ICollectionFixture<NavigationImportTestFixture>;
