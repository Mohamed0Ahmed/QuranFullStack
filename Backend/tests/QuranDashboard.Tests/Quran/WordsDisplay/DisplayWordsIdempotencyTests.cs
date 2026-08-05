using System.Globalization;
using System.Text;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsIdempotencyTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsIdempotencyTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ForcedRebuildTwiceOnUnchangedSource_ProducesIdenticalDerivedTables()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var firstReportDir = Path.Combine(Path.GetTempPath(), $"words-display-idem-1-{Guid.NewGuid():N}");
        var secondReportDir = Path.Combine(Path.GetTempPath(), $"words-display-idem-2-{Guid.NewGuid():N}");
        var command = new RebuildDisplayWordsCommand(
            Force: true,
            ReportOutDir: firstReportDir,
            ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount);

        var firstResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = firstReportDir },
            CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotAfterFirstRun = await CaptureDerivedSnapshotAsync();

        var secondResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = secondReportDir },
            CancellationToken.None);
        secondResult.Succeeded.Should().BeTrue(secondResult.Message);

        var snapshotAfterSecondRun = await CaptureDerivedSnapshotAsync();

        snapshotAfterSecondRun.Should().BeEquivalentTo(snapshotAfterFirstRun);
        snapshotAfterSecondRun.Checksum.Should().Be(snapshotAfterFirstRun.Checksum);
    }

    private async Task<DerivedTablesSnapshot> CaptureDerivedSnapshotAsync()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var orderedTashkeel = await dbContext.QuranWordsOrderedTashkeel
            .AsNoTracking()
            .OrderBy(row => row.WordOrderInMushaf)
            .Select(row => new OrderedTashkeelProjection(
                row.WordOrderInMushaf,
                row.QuranWordId,
                row.Location,
                row.VerseKey,
                row.SurahNumber,
                row.AyahNumber,
                row.PageNumber,
                row.LineNumber,
                row.WordOrderInAyah,
                row.WordOrderInSurah,
                row.TextUthmani,
                row.TextUthmaniSimple,
                row.TextImlaeiSimple,
                row.OccurrencesCount,
                row.AyahsCount,
                row.SurahsCount))
            .ToListAsync();

        var orderedSimple = await dbContext.QuranWordsOrderedSimple
            .AsNoTracking()
            .OrderBy(row => row.WordOrderInMushaf)
            .Select(row => new OrderedSimpleProjection(
                row.WordOrderInMushaf,
                row.QuranWordId,
                row.Location,
                row.VerseKey,
                row.SurahNumber,
                row.AyahNumber,
                row.PageNumber,
                row.LineNumber,
                row.WordOrderInAyah,
                row.WordOrderInSurah,
                row.WordKeyImlaeiSimple,
                row.TextUthmaniSimple,
                row.TextImlaeiSimple,
                row.OccurrencesCount,
                row.AyahsCount,
                row.SurahsCount))
            .ToListAsync();

        var uniqueTashkeel = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .ThenBy(row => row.TextUthmani)
            .Select(row => new UniqueTashkeelProjection(
                row.Id,
                row.TextUthmani,
                row.TextUthmaniSimple,
                row.TextImlaeiSimple,
                row.OccurrencesCount,
                row.AyahsCount,
                row.SurahsCount,
                row.FirstQuranWordId,
                row.FirstLocation,
                row.FirstSurahNumber,
                row.FirstAyahNumber,
                row.FirstWordOrderInMushaf,
                row.FirstPageNumber,
                row.FirstLineNumber))
            .ToListAsync();

        var uniqueSimple = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .ThenBy(row => row.WordKeyImlaeiSimple)
            .Select(row => new UniqueSimpleProjection(
                row.Id,
                row.WordKeyImlaeiSimple,
                row.TextUthmani,
                row.TextUthmaniSimple,
                row.TextImlaeiSimple,
                row.QpcGlyph,
                row.OccurrencesCount,
                row.AyahsCount,
                row.SurahsCount,
                row.FirstQuranWordId,
                row.FirstLocation,
                row.FirstSurahNumber,
                row.FirstAyahNumber,
                row.FirstWordOrderInMushaf,
                row.FirstPageNumber,
                row.FirstLineNumber))
            .ToListAsync();

        return new DerivedTablesSnapshot(orderedTashkeel, orderedSimple, uniqueTashkeel, uniqueSimple);
    }

    private sealed record DerivedTablesSnapshot(
        IReadOnlyList<OrderedTashkeelProjection> OrderedTashkeel,
        IReadOnlyList<OrderedSimpleProjection> OrderedSimple,
        IReadOnlyList<UniqueTashkeelProjection> UniqueTashkeel,
        IReadOnlyList<UniqueSimpleProjection> UniqueSimple)
    {
        public string Checksum { get; } = ComputeChecksum(OrderedTashkeel, OrderedSimple, UniqueTashkeel, UniqueSimple);

        private static string ComputeChecksum(
            IReadOnlyList<OrderedTashkeelProjection> orderedTashkeel,
            IReadOnlyList<OrderedSimpleProjection> orderedSimple,
            IReadOnlyList<UniqueTashkeelProjection> uniqueTashkeel,
            IReadOnlyList<UniqueSimpleProjection> uniqueSimple)
        {
            var builder = new StringBuilder();
            AppendLines(builder, orderedTashkeel);
            AppendLines(builder, orderedSimple);
            AppendLines(builder, uniqueTashkeel);
            AppendLines(builder, uniqueSimple);

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(bytes);
        }

        private static void AppendLines<T>(StringBuilder builder, IReadOnlyList<T> rows)
        {
            foreach (var row in rows)
            {
                builder.AppendLine(FormatRow(row));
            }
        }

        private static string FormatRow<T>(T row)
        {
            return string.Join(
                "|",
                typeof(T).GetProperties().Select(property =>
                    Convert.ToString(property.GetValue(row), CultureInfo.InvariantCulture) ?? string.Empty));
        }
    }

    private sealed record OrderedTashkeelProjection(
        int WordOrderInMushaf,
        int QuranWordId,
        string Location,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        short PageNumber,
        short LineNumber,
        short WordOrderInAyah,
        short WordOrderInSurah,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount);

    private sealed record OrderedSimpleProjection(
        int WordOrderInMushaf,
        int QuranWordId,
        string Location,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        short PageNumber,
        short LineNumber,
        short WordOrderInAyah,
        short WordOrderInSurah,
        string WordKeyImlaeiSimple,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount);

    private sealed record UniqueTashkeelProjection(
        int Id,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        int FirstQuranWordId,
        string FirstLocation,
        short FirstSurahNumber,
        short FirstAyahNumber,
        int FirstWordOrderInMushaf,
        short FirstPageNumber,
        short FirstLineNumber);

    private sealed record UniqueSimpleProjection(
        int Id,
        string WordKeyImlaeiSimple,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        string QpcGlyph,
        int OccurrencesCount,
        short AyahsCount,
        short SurahsCount,
        int FirstQuranWordId,
        string FirstLocation,
        short FirstSurahNumber,
        short FirstAyahNumber,
        int FirstWordOrderInMushaf,
        short FirstPageNumber,
        short FirstLineNumber);
}
