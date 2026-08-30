using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal sealed partial class QuranTopicsBookImporter(QuranDashboardDbContext db)
{
    private static readonly string LockSql =
        $"LOCK TABLE {string.Join(", ", QuranTopicsBookContract.EmptyTargetTables.Select(table => $"public.{table}"))} IN ACCESS EXCLUSIVE MODE";

    internal async Task<QuranTopicsBookImportResult> ImportAsync(
        QuranTopicsBookSourcePackage package,
        int actorUserId,
        bool validateOnly,
        Func<CancellationToken, Task<bool>> sourceUnchanged,
        CancellationToken cancellationToken)
    {
        var checks = package.Checks.ToList();
        var warnings = package.Warnings.ToList();
        await ValidateActorAsync(actorUserId, cancellationToken);
        checks.Add("active-owner-actor");

        var ayahsByVerseKey = await ResolveAyahsAsync(package.Document, cancellationToken);
        checks.Add("all-verse-keys-resolved");

        if (validateOnly)
        {
            var nonEmptyTables = await ReadNonEmptyTargetTablesAsync(cancellationToken);
            if (nonEmptyTables.Count > 0)
            {
                warnings.Add($"TARGET-NOT-EMPTY: {string.Join(", ", nonEmptyTables)}");
            }

            checks.Add("database-validation-only");
            return new QuranTopicsBookImportResult(
                "pass",
                "false",
                package.Metrics,
                checks,
                warnings,
                []);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await LockTargetTablesAsync(cancellationToken);
        var occupiedTables = await ReadNonEmptyTargetTablesAsync(cancellationToken);
        if (occupiedTables.Count > 0)
        {
            throw new QuranTopicsBookImportException(
                $"Import requires all Abwab-owned target tables to be empty: {string.Join(", ", occupiedTables)}",
                checks,
                warnings);
        }

        checks.Add("target-tables-empty");
        var now = DateTimeOffset.UtcNow;
        var sectionsByKey = await InsertSectionsAsync(package.Document, actorUserId, now, cancellationToken);
        var doorsByKey = await InsertDoorsAsync(
            package.Document,
            sectionsByKey,
            actorUserId,
            now,
            cancellationToken);
        await InsertLinksAsync(
            package.Document,
            doorsByKey,
            ayahsByVerseKey,
            actorUserId,
            now,
            cancellationToken);

        await VerifyPersistedStateAsync(package.Metrics, cancellationToken);
        checks.Add("persisted-counts-exact");
        if (!await sourceUnchanged(cancellationToken))
        {
            throw new QuranTopicsBookImportException(
                "The source or checksum changed while the transaction was running.",
                checks,
                warnings);
        }

        checks.Add("source-unchanged-before-commit");
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            throw new QuranTopicsBookCommitUnknownException();
        }

        return new QuranTopicsBookImportResult(
            "pass",
            "true",
            package.Metrics,
            checks,
            warnings,
            []);
    }

    private async Task ValidateActorAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var valid = await db.AccessUsers.AsNoTracking()
            .Where(user => user.Id == actorUserId && user.Status == UserStatus.Active)
            .Join(
                db.AccessRoles.AsNoTracking().Where(role => role.Name == RoleNames.Owner),
                user => user.RoleId,
                role => role.Id,
                (_, _) => true)
            .AnyAsync(cancellationToken);
        if (!valid)
        {
            throw new QuranTopicsBookImportException(
                $"Actor user {actorUserId} must exist and be an active Owner.");
        }
    }

    private async Task<IReadOnlyDictionary<string, Ayah>> ResolveAyahsAsync(
        QuranTopicsBookDocument document,
        CancellationToken cancellationToken)
    {
        var requested = document.Sections
            .SelectMany(section => section.Doors)
            .SelectMany(door => door.AyahGroups)
            .SelectMany(group => group.VerseKeys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var resolved = await db.QuranAyahs.AsNoTracking()
            .Where(ayah => requested.Contains(ayah.VerseKey))
            .ToListAsync(cancellationToken);
        var byVerseKey = resolved.ToDictionary(ayah => ayah.VerseKey, StringComparer.Ordinal);
        var missing = requested.Where(verseKey => !byVerseKey.ContainsKey(verseKey)).ToArray();
        if (missing.Length > 0)
        {
            throw new QuranTopicsBookImportException(
                $"The Quran database is missing {missing.Length} requested verse keys: {string.Join(", ", missing.Take(20))}");
        }

        return byVerseKey;
    }

    private async Task LockTargetTablesAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(LockSql, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ReadNonEmptyTargetTablesAsync(CancellationToken cancellationToken)
    {
        var nonEmpty = new List<string>();
        foreach (var table in QuranTopicsBookContract.EmptyTargetTables)
        {
            var countSql = $"SELECT count(*)::integer AS \"Value\" FROM public.{table}";
            var count = await db.Database.SqlQueryRaw<int>(
                    countSql)
                .SingleAsync(cancellationToken);
            if (count != 0)
            {
                nonEmpty.Add($"{table}={count}");
            }
        }

        return nonEmpty;
    }

    private async Task VerifyPersistedStateAsync(
        QuranTopicsBookMetrics expected,
        CancellationToken cancellationToken)
    {
        var sectionCount = await db.AbwabSections.CountAsync(cancellationToken);
        var doorCount = await db.AbwabDoors.CountAsync(cancellationToken);
        var groupCount = await db.LinkingSourceContributions.CountAsync(cancellationToken);
        var unitCount = await db.LinkingUnits.CountAsync(cancellationToken);
        var mappingCount = await db.LinkingSourceContributionUnits.CountAsync(cancellationToken);
        var unitAyahCount = await db.LinkingUnitAyahs.CountAsync(cancellationToken);
        var projectionCount = await db.LinkingDoorAyahs.CountAsync(cancellationToken);
        var operationCount = await db.LinkingOperations.CountAsync(cancellationToken);
        var linkedDoorCount = await db.LinkingSourceContributions
            .Select(contribution => contribution.DoorId)
            .Distinct()
            .CountAsync(cancellationToken);
        var expectedProjectionCount = await db.LinkingUnitAyahs
            .Join(db.LinkingUnits, unitAyah => unitAyah.UnitId, unit => unit.Id, (unitAyah, unit) => new
            {
                unit.DoorId,
                unitAyah.AyahId,
            })
            .Distinct()
            .CountAsync(cancellationToken);
        if (sectionCount != expected.SectionCount
            || doorCount != expected.DoorCount
            || groupCount != expected.AyahGroupCount
            || unitCount != expected.AyahGroupCount
            || mappingCount != expected.AyahGroupCount
            || unitAyahCount != expected.AyahReferenceCount
            || operationCount != linkedDoorCount
            || projectionCount != expectedProjectionCount
            || await db.LinkingDoorAyahWords.AnyAsync(cancellationToken)
            || await db.LinkingUnitAyahWords.AnyAsync(cancellationToken))
        {
            throw new QuranTopicsBookImportException("Post-import table counts or full-ayah projections are not exact.");
        }
    }

    private sealed record PreparedGroup(
        LinkingSourceContribution Contribution,
        LinkingUnit Unit,
        IReadOnlyList<Ayah> Ayahs);
}
