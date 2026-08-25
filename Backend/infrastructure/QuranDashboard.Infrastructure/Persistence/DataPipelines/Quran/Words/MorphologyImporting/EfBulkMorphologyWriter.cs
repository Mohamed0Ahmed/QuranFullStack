using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

public sealed class EfBulkMorphologyWriter : IMorphologyImportWriter
{
    private const string PassVerdict = MorphologyImportConstants.PassVerdict;
    private const string FailVerdict = MorphologyImportConstants.FailVerdict;
    private const string HardSeverity = MorphologyImportConstants.HardSeverity;

    private const string AnyTargetTableHasDataSql =
        """
        SELECT EXISTS (SELECT 1 FROM quran_word_morphology)
            OR EXISTS (SELECT 1 FROM quran_word_morphology_segments)
            OR EXISTS (SELECT 1 FROM quran_roots)
            OR EXISTS (SELECT 1 FROM quran_lemmas)
            OR EXISTS (SELECT 1 FROM quran_lemma_analyses)
            OR EXISTS (SELECT 1 FROM quran_stems)
            OR EXISTS (SELECT 1 FROM quran_pos_tags)
        """;

    private readonly QuranDashboardDbContext dbContext;
    private readonly SegmentArabicRenderer renderer;
    private readonly IWordLemmaNormalizationReader normalizationReader;
    private readonly ISegmentStemCorrectionReader segmentStemCorrectionReader;
    private readonly ILinkingDataRevisionWriterStore revisionStore;

    public EfBulkMorphologyWriter(
        QuranDashboardDbContext dbContext,
        SegmentArabicRenderer renderer,
        IWordLemmaNormalizationReader normalizationReader,
        ISegmentStemCorrectionReader segmentStemCorrectionReader,
        ILinkingDataRevisionWriterStore? revisionStore = null)
    {
        this.dbContext = dbContext;
        this.renderer = renderer;
        this.normalizationReader = normalizationReader;
        this.segmentStemCorrectionReader = segmentStemCorrectionReader;
        this.revisionStore = revisionStore ?? new LinkingDataRevisionStore();
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.WordMorphologies.AnyAsync(ct)
            || await dbContext.WordMorphologySegments.AnyAsync(ct)
            || await dbContext.QuranRoots.AnyAsync(ct)
            || await dbContext.QuranLemmas.AnyAsync(ct)
            || await dbContext.QuranLemmaAnalyses.AnyAsync(ct)
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

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for morphology import.");
        }

        var isLegacySource = source.SourceKind == MorphologyImportSourceKind.Legacy;
        var normalization = isLegacySource ? normalizationReader.Load() : null;
        var segmentStemCorrection = isLegacySource ? segmentStemCorrectionReader.Load() : null;

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            await revisionStore.LockForWriteAsync(npgsqlConnection, transaction, ct);

            if (!force && await AnyTargetTableHasDataAsync(npgsqlConnection, transaction, ct))
            {
                throw new InvalidOperationException(MorphologyInvariants.TargetsNotEmpty);
            }

            var wordIdsByLocation = await ReadReadableWordIdsAsync(npgsqlConnection, transaction, ct);

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
            await MorphologyBulkCopier.CopyLemmaAnalysesAsync(npgsqlConnection, source, ct);
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
                "local source and correction artifacts match their captured SHA-256 values before and after run",
                sourceUnchanged ? "unchanged" : "changed",
                sourceUnchanged));

            var warnings = MorphologyImportReportBuilder.BuildWarnings(totals, source);

            var hardChecks = checks.Where(check => check.Severity == HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (allHardPassed)
            {
                await revisionStore.IncrementAsync(npgsqlConnection, transaction, ct);
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
                    source.CorrectionSummary,
                    source.RootFallbackSummary);
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
                source.CorrectionSummary,
                source.RootFallbackSummary);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<bool> AnyTargetTableHasDataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(AnyTargetTableHasDataSql, connection, transaction);
        command.CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds;
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static async Task<Dictionary<string, int>> ReadReadableWordIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        const string sql = "SELECT location, id FROM quran_words WHERE is_ayah_marker = false";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        while (await reader.ReadAsync(ct))
        {
            result.Add(reader.GetString(0), reader.GetInt32(1));
        }

        return result;
    }
}
