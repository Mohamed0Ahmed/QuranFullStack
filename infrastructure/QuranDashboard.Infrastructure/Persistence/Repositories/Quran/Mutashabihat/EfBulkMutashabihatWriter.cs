using System.Data;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

public sealed class EfBulkMutashabihatWriter : IMutashabihatImportWriter
{
    private const string PassVerdict = MutashabihatImportConstants.PassVerdict;
    private const string FailVerdict = MutashabihatImportConstants.FailVerdict;
    private const string HardSeverity = MutashabihatImportConstants.HardSeverity;

    private readonly QuranDashboardDbContext dbContext;
    private readonly MutashabihatImportSession importSession;

    public EfBulkMutashabihatWriter(
        QuranDashboardDbContext dbContext,
        MutashabihatImportSession importSession)
    {
        this.dbContext = dbContext;
        this.importSession = importSession;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.MutashabihatGroups.AnyAsync(ct)
            || await dbContext.MutashabihatOccurrences.AnyAsync(ct)
            || await dbContext.SimilarAyahLinks.AnyAsync(ct);
    }

    public async Task<MutashabihatImportResult> ImportAsync(
        MutashabihatSourceData source,
        bool force,
        MutashabihatExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(MutashabihatInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for mutashabihat import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await MutashabihatCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, MutashabihatSql.TruncateMutashabihatTables, ct);
            }

            var loadTimeChecks = MutashabihatValidationRunner.BuildLoadTimeChecks(
                importSession.RawOccurrenceCount, expected);
            var preCopyChecks = MutashabihatValidationRunner.BuildPreCopyHardChecks(source);
            if (preCopyChecks.Any(check => !check.Passed))
            {
                await transaction.RollbackAsync(ct);

                var preCopyTotals = BuildTotals(source, importSession.RawOccurrenceCount);
                var failedChecks = loadTimeChecks.Concat(preCopyChecks).ToList();
                var preCopyErrors = preCopyChecks
                    .Where(check => !check.Passed)
                    .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                    .ToList();

                return new MutashabihatImportResult(
                    runAtUtc,
                    FailVerdict,
                    Persisted: false,
                    force,
                    preCopyTotals,
                    failedChecks,
                    Warnings: [],
                    preCopyErrors,
                    InfoNotes: ["Pre-COPY hard check failed; no mutashabihat rows were persisted."]);
            }

            var groupIdsBySourceGroupId = await MutashabihatBulkCopier.CopyGroupsAsync(
                npgsqlConnection, transaction, source, ct);
            await MutashabihatBulkCopier.CopyOccurrencesAsync(
                npgsqlConnection, source, groupIdsBySourceGroupId, ct);
            await MutashabihatBulkCopier.CopyLinksAsync(npgsqlConnection, source, ct);

            var totals = BuildTotals(source, importSession.RawOccurrenceCount);
            var checks = loadTimeChecks;
            checks.AddRange(await MutashabihatValidationRunner.RunPostCopyHardChecksAsync(
                npgsqlConnection, transaction, expected, ct));

            var sourceUnchanged = await sourceUnchangedCheck(ct);
            checks.Add(new MutashabihatCheckResult(
                MutashabihatInvariants.CheckSourceUnchanged,
                HardSeverity,
                "local source files match manifest.json size/sha256 before and after run",
                sourceUnchanged ? "unchanged" : "changed",
                sourceUnchanged));

            var hardChecks = checks.Where(check => check.Severity == HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (allHardPassed)
            {
                await transaction.CommitAsync(ct);

                return new MutashabihatImportResult(
                    runAtUtc,
                    PassVerdict,
                    Persisted: true,
                    force,
                    totals,
                    checks,
                    Warnings: [],
                    Errors: [],
                    InfoNotes: ["Mutashabihat import committed; all hard checks passed."]);
            }

            await transaction.RollbackAsync(ct);

            var errors = hardChecks
                .Where(check => !check.Passed)
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList();

            return new MutashabihatImportResult(
                runAtUtc,
                FailVerdict,
                Persisted: false,
                force,
                totals,
                checks,
                Warnings: [],
                errors,
                InfoNotes: ["Totals reflect the attempted import before rollback; no mutashabihat rows were persisted."]);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static MutashabihatImportTotals BuildTotals(MutashabihatSourceData source, int rawOccurrenceCount)
    {
        var storedOccurrences = source.Groups.Sum(group => group.Occurrences.Count);
        var distinctAyahs = source.Groups
            .SelectMany(group => group.Occurrences)
            .Select(occurrence => occurrence.AyahId)
            .Concat(source.Groups.Select(group => group.RepresentativeAyahId))
            .Concat(source.Links.Select(link => link.SourceAyahId))
            .Concat(source.Links.Select(link => link.TargetAyahId))
            .Distinct()
            .Count();

        return new MutashabihatImportTotals(
            source.Groups.Count,
            rawOccurrenceCount,
            storedOccurrences,
            source.Links.Count,
            source.Links.Select(link => link.SourceAyahId).Distinct().Count(),
            distinctAyahs);
    }
}
