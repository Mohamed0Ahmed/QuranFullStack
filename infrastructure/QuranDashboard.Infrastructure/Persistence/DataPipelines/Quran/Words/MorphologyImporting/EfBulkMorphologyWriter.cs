using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

public sealed class EfBulkMorphologyWriter : IMorphologyImportWriter
{
    private const string PassVerdict = MorphologyImportConstants.PassVerdict;
    private const string FailVerdict = MorphologyImportConstants.FailVerdict;
    private const string HardSeverity = MorphologyImportConstants.HardSeverity;

    private readonly QuranDashboardDbContext dbContext;
    private readonly SegmentArabicRenderer renderer;
    private readonly IWordLemmaNormalizationReader normalizationReader;
    private readonly ISegmentStemCorrectionReader segmentStemCorrectionReader;

    public EfBulkMorphologyWriter(
        QuranDashboardDbContext dbContext,
        SegmentArabicRenderer renderer,
        IWordLemmaNormalizationReader normalizationReader,
        ISegmentStemCorrectionReader segmentStemCorrectionReader)
    {
        this.dbContext = dbContext;
        this.renderer = renderer;
        this.normalizationReader = normalizationReader;
        this.segmentStemCorrectionReader = segmentStemCorrectionReader;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.WordMorphologies.AnyAsync(ct)
            || await dbContext.WordMorphologySegments.AnyAsync(ct)
            || await dbContext.QuranRoots.AnyAsync(ct)
            || await dbContext.QuranLemmas.AnyAsync(ct)
            || await dbContext.QuranStems.AnyAsync(ct)
            || await dbContext.PosTags.AnyAsync(ct);
    }

    public async Task<MorphologyImportResult> ImportAsync(
        MorphologySourceData source,
        bool force,
        int expectedReadableWords,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(MorphologyInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var wordIdsByLocation = await ReadReadableWordIdsAsync(ct);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for morphology import.");
        }

        var normalization = normalizationReader.Load();
        var segmentStemCorrection = segmentStemCorrectionReader.Load();

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await MorphologyCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, MorphologySql.TruncateMorphologyTables, ct);
            }

            var posResolvesCheck = MorphologyValidationRunner.BuildPosResolvesCheck(source);
            if (!posResolvesCheck.Passed)
            {
                await transaction.RollbackAsync(ct);
                return MorphologyImportReportBuilder.BuildUnknownPosResult(runAtUtc, force, source, posResolvesCheck);
            }

            await MorphologyBulkCopier.CopyPosTagsAsync(npgsqlConnection, ct);
            await MorphologyBulkCopier.CopyRootsAsync(npgsqlConnection, source, ct);
            await MorphologyBulkCopier.CopyLemmasAsync(npgsqlConnection, source, ct);
            await MorphologyBulkCopier.CopyStemsAsync(npgsqlConnection, source, ct);
            await MorphologyBulkCopier.CopyMorphologyAsync(npgsqlConnection, source, wordIdsByLocation, ct);
            await MorphologyBulkCopier.CopySegmentsAsync(npgsqlConnection, source, wordIdsByLocation, ct);

            var totals = await MorphologyImportReportBuilder.GatherTotalsAsync(npgsqlConnection, transaction, ct);
            var checks = await MorphologyValidationRunner.RunAllHardChecksAsync(
                npgsqlConnection,
                transaction,
                expectedReadableWords,
                source,
                normalization,
                segmentStemCorrection,
                renderer,
                ct);
            checks.Add(posResolvesCheck);

            var sourceUnchanged = await sourceUnchangedCheck(ct);
            checks.Add(new MorphologyCheckResult(
                MorphologyInvariants.CheckSourceUnchanged,
                HardSeverity,
                "local source files match manifest.json size/sha256 before and after run",
                sourceUnchanged ? "unchanged" : "changed",
                sourceUnchanged));

            var warnings = MorphologyImportReportBuilder.BuildWarnings(totals, source);

            var hardChecks = checks.Where(check => check.Severity == HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (allHardPassed)
            {
                await transaction.CommitAsync(ct);

                return new MorphologyImportResult(
                    runAtUtc,
                    PassVerdict,
                    Persisted: true,
                    force,
                    totals,
                    checks,
                    warnings,
                    Errors: [],
                    InfoNotes: ["Morphology import committed; all hard checks passed."],
                    source.CorrectionSummary);
            }

            await transaction.RollbackAsync(ct);

            var errors = hardChecks
                .Where(check => !check.Passed)
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList();

            return new MorphologyImportResult(
                runAtUtc,
                FailVerdict,
                Persisted: false,
                force,
                totals,
                checks,
                warnings,
                errors,
                InfoNotes: ["Totals reflect the attempted import before rollback; no morphology rows were persisted."],
                source.CorrectionSummary);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Dictionary<string, int>> ReadReadableWordIdsAsync(CancellationToken ct)
    {
        var rows = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker)
            .Select(word => new { word.Location, word.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.Location, row => row.Id, StringComparer.Ordinal);
    }
}
