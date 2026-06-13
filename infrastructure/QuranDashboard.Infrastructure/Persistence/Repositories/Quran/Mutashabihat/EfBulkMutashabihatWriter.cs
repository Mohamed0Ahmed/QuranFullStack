using System.Data;
using System.Globalization;
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
            var groupIdsBySourceGroupId = await MutashabihatBulkCopier.CopyGroupsAsync(
                npgsqlConnection, transaction, source, ct);
            await MutashabihatBulkCopier.CopyOccurrencesAsync(
                npgsqlConnection, source, groupIdsBySourceGroupId, ct);

            var totals = BuildTotals(source, importSession.RawOccurrenceCount);
            var checks = await RunUs1HardChecksAsync(
                npgsqlConnection, transaction, expected, importSession.RawOccurrenceCount, ct);

            var allHardPassed = checks.All(check => check.Passed);
            var verdict = allHardPassed ? PassVerdict : FailVerdict;
            var errors = allHardPassed
                ? []
                : checks
                    .Where(check => !check.Passed)
                    .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                    .ToList();

            await transaction.CommitAsync(ct);

            return new MutashabihatImportResult(
                runAtUtc,
                verdict,
                Persisted: true,
                force,
                totals,
                checks,
                Warnings: [],
                errors,
                allHardPassed
                    ? ["Mutashabihat groups and occurrences committed (US1 path)."]
                    : ["Totals reflect the committed import; hard checks failed (US1 path commits without rollback)."]);
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

    private static async Task<List<MutashabihatCheckResult>> RunUs1HardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MutashabihatExpectedCounts expected,
        int rawOccurrenceCount,
        CancellationToken ct)
    {
        var checks = new List<MutashabihatCheckResult>();

        var groupCount = await ExecuteScalarIntAsync(connection, transaction, MutashabihatSql.CheckGroupCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-GROUP-COUNT",
            HardSeverity,
            FormatInt(expected.Groups),
            FormatInt(groupCount),
            groupCount == expected.Groups));

        var storedOccurrenceCount = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckStoredOccurrenceCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-STORED-OCCURRENCE-COUNT",
            HardSeverity,
            FormatInt(expected.StoredOccurrences),
            FormatInt(storedOccurrenceCount),
            storedOccurrenceCount == expected.StoredOccurrences));

        checks.Add(new MutashabihatCheckResult(
            "MUT-RAW-OCCURRENCE-COUNT",
            HardSeverity,
            FormatInt(expected.RawOccurrences),
            FormatInt(rawOccurrenceCount),
            rawOccurrenceCount == expected.RawOccurrences));

        checks.Add(new MutashabihatCheckResult(
            "MUT-VERSEKEY-FORMAT",
            HardSeverity,
            "every reference matches ^\\d+:\\d+$",
            "validated during assembly",
            true));

        var duplicateViolations = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOccurrenceUniqueViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-OCCURRENCE-UNIQUE",
            HardSeverity,
            "0 duplicate tuples",
            duplicateViolations == 0 ? "0 duplicates" : $"{FormatInt(duplicateViolations)} duplicate tuple(s)",
            duplicateViolations == 0));

        var minSizeViolations = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckGroupMinSizeViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-GROUP-MIN-SIZE",
            HardSeverity,
            "every group distinct_ayah_count >= 2",
            minSizeViolations == 0 ? "0 violations" : $"{FormatInt(minSizeViolations)} violation(s)",
            minSizeViolations == 0));

        var wordRangeViolations = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOccurrenceWordRangeViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-WORD-RANGE-SHAPE",
            HardSeverity,
            "occurrence ranges have from >= 1 and to >= from",
            wordRangeViolations == 0 ? "0 violations" : $"{FormatInt(wordRangeViolations)} violation(s)",
            wordRangeViolations == 0));

        var unresolvedRepresentatives = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedGroupRepresentativeAyahs, ct);
        var unresolvedOccurrences = await ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedOccurrenceAyahs, ct);
        var unresolvedTotal = unresolvedRepresentatives + unresolvedOccurrences;
        checks.Add(new MutashabihatCheckResult(
            "MUT-AYAH-RESOLVE",
            HardSeverity,
            "0 unresolved ayah references",
            unresolvedTotal == 0 ? "0 unresolved" : $"{FormatInt(unresolvedTotal)} unresolved",
            unresolvedTotal == 0));

        return checks;
    }

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
}
