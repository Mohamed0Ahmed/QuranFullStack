using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Mutashabihat;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Tafsirs;
using QuranDashboard.Domain.Quran.Translations;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

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
        await dbContext.Database.ExecuteSqlRawAsync(NavigationMetadataSql.ClearNavigationData);
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

    public async Task SeedIsolationCompanionDataAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.QuranMushafLines.Add(new MushafLine
        {
            PageNumber = 901,
            LineNumber = 1,
            LineType = MushafLineType.Ayah,
            IsCentered = false,
            WordsCount = 1
        });

        dbContext.QuranWords.Add(new QuranWord
        {
            Id = 1,
            Location = "901:1:1",
            AyahId = 1,
            SurahNumber = NavigationSyntheticSeed.SyntheticSurahNumber,
            AyahNumber = 1,
            WordNumber = 1,
            PageNumber = 901,
            LineNumber = 1,
            LineWordOrder = 1,
            QpcGlyph = "TEST-GLYPH",
            TextUthmani = "اختبار-كلمة",
            TextUthmaniSimple = "اختبار-كلمة",
            TextImlaeiSimple = "اختبار-كلمة",
            WordKeyImlaeiSimple = "901:1:1",
            IsAyahMarker = false
        });

        dbContext.TranslationSources.Add(new TranslationSource
        {
            SourceKey = "en-test-isolation",
            LanguageCode = "en",
            LanguageNameEn = "English",
            LanguageNameAr = "الإنجليزية",
            Direction = "ltr",
            TranslationType = "simple",
            DisplayNameEn = "Isolation Translation",
            DisplayNameAr = "ترجمة-عزل",
            ContentCoverageCount = (short)TranslationInvariants.ExpectedAyahsPerSource
        });

        await dbContext.SaveChangesAsync();

        dbContext.TranslationAyahEntries.Add(new TranslationAyahEntry
        {
            SourceId = dbContext.TranslationSources.Single().Id,
            AyahId = 1,
            VerseKey = NavigationSyntheticSeed.SyntheticVerseKey(1),
            Text = "SYNTHETIC-TRANSLATION-901:1"
        });

        dbContext.TafsirSources.Add(new TafsirSource
        {
            SourceKey = "test-isolation-tafsir",
            LanguageCode = "ar",
            LanguageNameAr = "العربية",
            LanguageNameEn = "Arabic",
            Direction = "rtl",
            DisplayNameAr = "تفسير-عزل",
            ShortNameAr = "تفسير-عزل",
            DisplayNameEn = "Isolation Tafsir",
            ShortNameEn = "Isolation Tafsir",
            ContributorType = "author",
            ResourceKind = "tafsir",
            TafsirKind = "simple",
            ContentCoverageCount = 6236,
            PackageFile = "synthetic.json",
            SourceFileOriginal = "synthetic.json",
            Sha256 = "abc123",
            FileSizeBytes = 10,
            LicenseStatus = "test",
            ProvenanceStatus = "test",
            ManifestMetadata = "{}",
            ImportedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var tafsirSourceId = dbContext.TafsirSources.Single().Id;
        dbContext.TafsirEntries.Add(new TafsirEntry
        {
            SourceId = tafsirSourceId,
            SourceEntryKey = "901:1",
            LeaderAyahId = 1,
            TafsirText = "SYNTHETIC-TAFSIR-901:1",
            CoveredAyahCount = 1,
            CoveredAyahKeys = """["901:1"]""",
            SourceShape = "flat",
            TextHash = "hash-901-1"
        });

        await dbContext.SaveChangesAsync();

        dbContext.TafsirAyahEntries.Add(new TafsirAyahEntry
        {
            SourceId = tafsirSourceId,
            AyahId = 1,
            TafsirEntryId = dbContext.TafsirEntries.Single().Id,
            VerseKey = NavigationSyntheticSeed.SyntheticVerseKey(1),
            SourceValueKind = "leader",
            SourceLeaderVerseKey = NavigationSyntheticSeed.SyntheticVerseKey(1),
            IsGroupLeader = true,
            SortOrder = 1
        });

        dbContext.MutashabihatGroups.Add(new MutashabihatGroup
        {
            SourceGroupId = 90101,
            RepresentativeAyahId = 1,
            RepresentativeWordFrom = 1,
            RepresentativeWordTo = 1,
            OccurrenceCount = 1,
            DistinctAyahCount = 1,
            DistinctSurahCount = 1
        });

        await dbContext.SaveChangesAsync();

        dbContext.MutashabihatOccurrences.Add(new MutashabihatOccurrence
        {
            GroupId = dbContext.MutashabihatGroups.Single().Id,
            AyahId = 1,
            WordFrom = 1,
            WordTo = 1,
            IsRepresentative = true
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task<NavigationIsolationBaseline> CaptureIsolationBaselineAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new NavigationIsolationBaseline(
            await dbContext.QuranSurahs.AsNoTracking().OrderBy(row => row.SurahNumber).ToListAsync(),
            await dbContext.QuranAyahs.AsNoTracking()
                .OrderBy(row => row.Id)
                .Select(row => new AyahIsolationRow(
                    row.Id,
                    row.VerseKey,
                    row.TextUthmani,
                    row.SurahNumber,
                    row.AyahNumber,
                    row.WordsCountSource,
                    row.WordsCountReal,
                    row.PageFrom,
                    row.PageTo))
                .ToListAsync(),
            await dbContext.QuranMushafPages.AsNoTracking().OrderBy(row => row.PageNumber).ToListAsync(),
            await dbContext.QuranMushafLines.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.QuranWords.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.TranslationSources.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.TranslationAyahEntries.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.TafsirSources.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.TafsirEntries.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.TafsirAyahEntries.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.MutashabihatGroups.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.MutashabihatOccurrences.AsNoTracking().OrderBy(row => row.Id).ToListAsync(),
            await dbContext.WordMorphologies.CountAsync(),
            await dbContext.WordMorphologySegments.CountAsync(),
            await dbContext.QuranRoots.CountAsync(),
            await dbContext.QuranLemmas.CountAsync(),
            await dbContext.QuranStems.CountAsync(),
            await dbContext.PosTags.CountAsync(),
            await dbContext.QuranI3rabRules.CountAsync());
    }

    public async Task<NavigationDataSnapshot> CaptureNavigationDataSnapshotAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new NavigationDataSnapshot(
            await dbContext.QuranJuzs.AsNoTracking().OrderBy(row => row.JuzNumber).ToListAsync(),
            await dbContext.QuranHizbs.AsNoTracking().OrderBy(row => row.HizbNumber).ToListAsync(),
            await dbContext.QuranRubs.AsNoTracking().OrderBy(row => row.RubNumber).ToListAsync(),
            await dbContext.QuranSajdas.AsNoTracking().OrderBy(row => row.SajdahNumber).ToListAsync(),
            await dbContext.QuranAyahs.AsNoTracking()
                .OrderBy(row => row.Id)
                .Select(row => new AyahNavigationRow(row.Id, row.JuzNumber, row.HizbNumber, row.RubNumber))
                .ToListAsync());
    }

    public async Task SeedOrphanedAyahNavigationColumnsAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            SET session_replication_role = replica;
            UPDATE quran_ayahs
            SET juz_number = 1
            WHERE id = 1;
            SET session_replication_role = DEFAULT;
            """);
    }
}

public sealed record NavigationTableSnapshot(
    int JuzRows,
    int HizbRows,
    int RubRows,
    int SajdaRows,
    int TaggedAyahRows);

public sealed record AyahIsolationRow(
    int Id,
    string VerseKey,
    string TextUthmani,
    short SurahNumber,
    short AyahNumber,
    short WordsCountSource,
    short WordsCountReal,
    short PageFrom,
    short PageTo);

public sealed record NavigationIsolationBaseline(
    IReadOnlyList<Surah> Surahs,
    IReadOnlyList<AyahIsolationRow> Ayahs,
    IReadOnlyList<MushafPage> Pages,
    IReadOnlyList<MushafLine> Lines,
    IReadOnlyList<QuranWord> Words,
    IReadOnlyList<TranslationSource> TranslationSources,
    IReadOnlyList<TranslationAyahEntry> TranslationEntries,
    IReadOnlyList<TafsirSource> TafsirSources,
    IReadOnlyList<TafsirEntry> TafsirEntries,
    IReadOnlyList<TafsirAyahEntry> TafsirAyahEntries,
    IReadOnlyList<MutashabihatGroup> MutashabihatGroups,
    IReadOnlyList<MutashabihatOccurrence> MutashabihatOccurrences,
    int WordMorphologyRows,
    int WordMorphologySegmentRows,
    int RootRows,
    int LemmaRows,
    int StemRows,
    int PosTagRows,
    int I3rabRuleRows);

public sealed record AyahNavigationRow(int Id, short? JuzNumber, short? HizbNumber, short? RubNumber);

public sealed record NavigationDataSnapshot(
    IReadOnlyList<Juz> Juz,
    IReadOnlyList<Hizb> Hizb,
    IReadOnlyList<Rub> Rub,
    IReadOnlyList<Sajda> Sajda,
    IReadOnlyList<AyahNavigationRow> AyahNavigation);

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
    IReadOnlyList<SyntheticNavigationSajdaSpec> Sajda,
    bool IncludeDecoyQuranTextFields = false);

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

    public static SyntheticNavigationPackageSpec ReportTestPackageSpec => DefaultPackageSpec with
    {
        Juz =
        [
            new SyntheticNavigationDivisionSpec(1, 99, SyntheticVerseKey(1), SyntheticVerseKey(3), new Dictionary<string, string> { ["901"] = "1-3" }),
            new SyntheticNavigationDivisionSpec(2, 3, SyntheticVerseKey(4), SyntheticVerseKey(6), new Dictionary<string, string> { ["901"] = "4-6" })
        ]
    };

    public static SyntheticNavigationPackageSpec DecoyQuranTextPackageSpec =>
        DefaultPackageSpec with { IncludeDecoyQuranTextFields = true };

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
