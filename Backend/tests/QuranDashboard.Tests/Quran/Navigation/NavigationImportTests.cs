using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationImportTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Import_tags_all_seeded_ayahs_persists_navigation_rows_and_writes_reports()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var reportWriter = new CommitOrderingReportWriter();
        await using var provider = fixture.CreateCallerDisposedServiceProvider(services =>
            services.AddSingleton<INavigationMetadataReportWriter>(reportWriter));
        await using var importScope = provider.CreateAsyncScope();
        var handler = importScope.ServiceProvider.GetRequiredService<ImportNavigationMetadataHandler>();
        var importTask = handler.HandleAsync(
            new ImportNavigationMetadataCommand(
                packageDir,
                Force: false,
                NavigationSyntheticSeed.DefaultTestExpectedCounts,
                reportDir),
            CancellationToken.None);

        ImportNavigationMetadataResult result;
        try
        {
            await reportWriter.ProvisionalWriteStarted;
            reportWriter.ProvisionalReport!.Persisted.Should().BeFalse();
            var provisionalSnapshot = await fixture.CaptureNavigationSnapshotAsync();
            provisionalSnapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));

            var provisionalJson = await File.ReadAllTextAsync(
                Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
            using (var provisionalDocument = JsonDocument.Parse(provisionalJson))
            {
                provisionalDocument.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
            }

            reportWriter.ReleaseProvisionalWrite();
            await reportWriter.FinalWriteStarted;
            reportWriter.FinalReport!.Persisted.Should().BeTrue();
            var committedSnapshot = await fixture.CaptureNavigationSnapshotAsync();
            committedSnapshot.Should().Be(new NavigationTableSnapshot(2, 2, 4, 2, 6));

            reportWriter.ReleaseFinalWrite();
            result = await importTask;
        }
        finally
        {
            reportWriter.ReleaseProvisionalWrite();
            reportWriter.ReleaseFinalWrite();
            if (!importTask.IsCompleted)
            {
                try
                {
                    await importTask;
                }
                catch
                {
                    // Preserve the assertion or read failure that triggered cleanup.
                }
            }
        }

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.Juz.Should().Be(2);
        result.Totals.Hizb.Should().Be(2);
        result.Totals.Rub.Should().Be(4);
        result.Totals.Sajda.Should().Be(2);
        result.Totals.AyahsTagged.Should().Be(6);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranJuzs.CountAsync()).Should().Be(2);
        (await dbContext.QuranHizbs.CountAsync()).Should().Be(2);
        (await dbContext.QuranRubs.CountAsync()).Should().Be(4);
        (await dbContext.QuranSajdas.CountAsync()).Should().Be(2);

        var ayahs = await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .ToListAsync();
        ayahs.Should().HaveCount(6);

        foreach (var (ayahId, juzNumber, hizbNumber, rubNumber) in NavigationSyntheticSeed.ExpectedAyahAssignments)
        {
            var ayah = ayahs.Single(row => row.Id == ayahId);
            ayah.JuzNumber.Should().Be(juzNumber, $"ayah {ayahId} juz");
            ayah.HizbNumber.Should().Be(hizbNumber, $"ayah {ayahId} hizb");
            ayah.RubNumber.Should().Be(rubNumber, $"ayah {ayahId} rub");
        }

        var sajdas = await dbContext.QuranSajdas
            .AsNoTracking()
            .OrderBy(sajda => sajda.SajdahNumber)
            .Select(sajda => new { sajda.SajdahNumber, sajda.VerseKey, sajda.SajdahType })
            .ToListAsync();

        sajdas.Should().Equal(
            NavigationSyntheticSeed.ExpectedSajdaRows.Select(row => new
            {
                SajdahNumber = row.SajdahNumber,
                VerseKey = row.VerseKey,
                SajdahType = string.Equals(row.SajdahType, "required", StringComparison.Ordinal)
                    ? SajdahType.Required
                    : SajdahType.Optional
            }));

        File.Exists(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        reportDocument.RootElement.GetProperty("verdict").GetString().Should().Be(NavigationImportConstants.AcceptedVerdict);
        reportDocument.RootElement.GetProperty("persisted").GetBoolean().Should().BeTrue();
        reportDocument.RootElement.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();
    }

    private sealed class CommitOrderingReportWriter : INavigationMetadataReportWriter
    {
        private readonly MarkdownJsonNavigationMetadataReportWriter inner = new();
        private readonly TaskCompletionSource provisionalWriteStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseProvisionalWrite =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource finalWriteStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseFinalWrite =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int writeCount;

        public Task ProvisionalWriteStarted => provisionalWriteStarted.Task;
        public Task FinalWriteStarted => finalWriteStarted.Task;
        public NavigationMetadataImportReport? ProvisionalReport { get; private set; }
        public NavigationMetadataImportReport? FinalReport { get; private set; }

        public async Task WriteAsync(
            NavigationMetadataImportReport report,
            string reportOutDir,
            CancellationToken ct)
        {
            var currentWrite = Interlocked.Increment(ref writeCount);
            if (currentWrite == 1)
            {
                ProvisionalReport = report;
                await inner.WriteAsync(report, reportOutDir, ct);
                provisionalWriteStarted.SetResult();
                await releaseProvisionalWrite.Task.WaitAsync(ct);
                return;
            }

            FinalReport = report;
            finalWriteStarted.SetResult();
            await releaseFinalWrite.Task.WaitAsync(ct);
            await inner.WriteAsync(report, reportOutDir, ct);
        }

        public void ReleaseProvisionalWrite() => releaseProvisionalWrite.TrySetResult();

        public void ReleaseFinalWrite() => releaseFinalWrite.TrySetResult();
    }
}
