using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

internal sealed class EfDoorLinkRecordsReader(QuranDashboardDbContext db) : IDoorLinkRecordsReader
{
    public async Task<DoorLinkRecordsReadResult> ReadRecordsAsync(
        int doorId,
        uint? expectedDoorVersion,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        var door = await ReadDoorAsync(doorId, cancellationToken);
        var invalidDoor = ValidateDoorForRecords(door, expectedDoorVersion);
        if (invalidDoor is not null)
        {
            return invalidDoor;
        }

        var liveUnits = LiveUnits(doorId);
        var totalCount = await liveUnits.CountAsync(cancellationToken);
        var rows = await liveUnits
            .OrderBy(unit => unit.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(unit => new RecordSummaryRow(
                unit.Id,
                unit.IsGrouped,
                db.LinkingUnitAyahs.Count(unitAyah => unitAyah.UnitId == unit.Id),
                (
                    from unitAyah in db.LinkingUnitAyahs
                    join word in db.LinkingUnitAyahWords on unitAyah.Id equals word.UnitAyahId
                    where unitAyah.UnitId == unit.Id
                    select word).Count(),
                (
                    from unitAyah in db.LinkingUnitAyahs
                    join description in db.LinkingUnitAyahDescriptions
                        on unitAyah.Id equals description.UnitAyahId
                    where unitAyah.UnitId == unit.Id
                    select description).Count(),
                (
                    from unitAyah in db.LinkingUnitAyahs
                    join ayah in db.QuranAyahs on unitAyah.AyahId equals ayah.Id
                    where unitAyah.UnitId == unit.Id
                    orderby unitAyah.OrderValue, unitAyah.Id
                    select ayah.VerseKey).First(),
                (
                    from unitAyah in db.LinkingUnitAyahs
                    join ayah in db.QuranAyahs on unitAyah.AyahId equals ayah.Id
                    where unitAyah.UnitId == unit.Id
                    orderby unitAyah.OrderValue descending, unitAyah.Id descending
                    select ayah.VerseKey).First()))
            .ToListAsync(cancellationToken);

        var unitIds = rows.Select(row => row.UnitId).ToList();
        var sourceLabelRows = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                where unitIds.Contains(mapping.UnitId)
                    && contribution.DoorId == doorId
                    && contribution.DeletedAtUtc == null
                select new { mapping.UnitId, contribution.Label })
            .Distinct()
            .ToListAsync(cancellationToken);
        var sourceLabelsByUnit = sourceLabelRows
            .GroupBy(row => row.UnitId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group
                    .Select(row => row.Label)
                    .Order(StringComparer.Ordinal)]);

        var items = rows
            .Select(row => new DoorLinkRecordSummaryDto(
                row.UnitId,
                row.IsGrouped,
                row.AyahCount,
                row.SelectedWordCount,
                row.DescriptionCount,
                sourceLabelsByUnit.GetValueOrDefault(row.UnitId, []),
                row.FirstVerseKey,
                row.LastVerseKey))
            .ToList();

        var result = new DoorLinkRecordsReadResult.Success(new DoorLinkRecordsPageDto(
            doorId,
            door!.Version,
            page,
            pageSize,
            totalCount,
            items));
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DoorLinkAyahsReadResult> ReadAyahsAsync(
        int doorId,
        long unitId,
        uint expectedDoorVersion,
        long linkingDataRevision,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var door = await ReadDoorAsync(doorId, cancellationToken);
        var invalidDoor = ValidateDoorForAyahs(door, expectedDoorVersion);
        if (invalidDoor is not null)
        {
            return invalidDoor;
        }

        var unit = await LiveUnits(doorId)
            .Where(candidate => candidate.Id == unitId)
            .Select(candidate => new UnitRow(candidate.Id, candidate.IsGrouped))
            .SingleOrDefaultAsync(cancellationToken);
        if (unit is null)
        {
            return new DoorLinkAyahsReadResult.UnitNotFound();
        }

        var unitAyahs = db.LinkingUnitAyahs.AsNoTracking()
            .Where(unitAyah => unitAyah.UnitId == unitId);
        var totalCount = await unitAyahs.CountAsync(cancellationToken);
        var pageRows = await unitAyahs
            .OrderBy(unitAyah => unitAyah.OrderValue)
            .ThenBy(unitAyah => unitAyah.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(unitAyah => new UnitAyahRow(unitAyah.Id, unitAyah.AyahId))
            .ToListAsync(cancellationToken);
        var unitAyahIds = pageRows.Select(row => row.UnitAyahId).ToList();
        var ayahIds = pageRows.Select(row => row.AyahId).ToList();

        var selectedWordRows = await (
                from word in db.LinkingUnitAyahWords.AsNoTracking()
                join quranWord in db.QuranWords.AsNoTracking()
                    on word.QuranWordId equals quranWord.Id
                where unitAyahIds.Contains(word.UnitAyahId)
                orderby word.UnitAyahId, quranWord.WordNumber, quranWord.Id
                select new SelectedWordRow(word.UnitAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);
        var selectedWordIdsByUnitAyah = selectedWordRows
            .GroupBy(row => row.UnitAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)[.. group.Select(row => row.QuranWordId)]);
        var selectedWordIdsByAyah = pageRows.ToDictionary(
            row => row.AyahId,
            row => selectedWordIdsByUnitAyah.GetValueOrDefault(row.UnitAyahId, []));

        var descriptionRows = await db.LinkingUnitAyahDescriptions.AsNoTracking()
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .ThenBy(description => description.Id)
            .Select(description => new DescriptionRow(description.UnitAyahId, description.Body))
            .ToListAsync(cancellationToken);
        var descriptionsByUnitAyah = descriptionRows
            .GroupBy(row => row.UnitAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group.Select(row => row.Body)]);

        var ayahMetaRows = await LinkingAyahHydration.LoadByIdsAsync(db, ayahIds, cancellationToken);
        var ayahMetaById = ayahMetaRows.ToDictionary(row => row.AyahId);
        var orderedAyahMeta = pageRows.Select(row => ayahMetaById[row.AyahId]).ToList();
        var hydrated = await LinkingAyahHydration.ProjectAsync(
            db,
            orderedAyahMeta,
            selectedWordIdsByAyah,
            true,
            cancellationToken);
        var hydratedByAyah = hydrated.ToDictionary(ayah => ayah.AyahId);

        var items = pageRows.Select(row =>
        {
            var ayah = hydratedByAyah[row.AyahId];
            return new DoorLinkAyahDto(
                ayah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                ayah.SurahNameArabic,
                ayah.PageFrom,
                ayah.PageTo,
                selectedWordIdsByUnitAyah.GetValueOrDefault(row.UnitAyahId, []),
                descriptionsByUnitAyah.GetValueOrDefault(row.UnitAyahId, []),
                ayah.Words);
        }).ToList();

        return new DoorLinkAyahsReadResult.Success(new DoorLinkAyahsPageDto(
            doorId,
            door!.Version,
            unit.Id,
            unit.IsGrouped,
            linkingDataRevision,
            page,
            pageSize,
            totalCount,
            items));
    }

    private IQueryable<Domain.Linking.LinkingUnit> LiveUnits(int doorId) =>
        db.LinkingUnits.AsNoTracking()
            .Where(unit => unit.DoorId == doorId)
            .Where(unit =>
                db.LinkingSourceContributionUnits.AsNoTracking()
                    .Where(mapping => mapping.UnitId == unit.Id)
                    .Join(
                        db.LinkingSourceContributions.AsNoTracking()
                            .Where(contribution =>
                                contribution.DoorId == doorId
                                && contribution.DeletedAtUtc == null),
                        mapping => mapping.SourceContributionId,
                        contribution => contribution.Id,
                        (_, _) => 1)
                    .Any());

    private async Task<DoorRow?> ReadDoorAsync(int doorId, CancellationToken cancellationToken) =>
        await db.AbwabDoors.AsNoTracking()
            .Where(door => door.Id == doorId)
            .Select(door => new DoorRow(door.Version, door.DeletedAtUtc != null))
            .SingleOrDefaultAsync(cancellationToken);

    private static DoorLinkRecordsReadResult? ValidateDoorForRecords(
        DoorRow? door,
        uint? expectedDoorVersion)
    {
        if (door is null)
        {
            return new DoorLinkRecordsReadResult.DoorNotFound();
        }

        if (door.IsArchived)
        {
            return new DoorLinkRecordsReadResult.DoorArchived();
        }

        return expectedDoorVersion is uint expected && expected != door.Version
            ? new DoorLinkRecordsReadResult.DoorVersionStale()
            : null;
    }

    private static DoorLinkAyahsReadResult? ValidateDoorForAyahs(
        DoorRow? door,
        uint expectedDoorVersion)
    {
        if (door is null)
        {
            return new DoorLinkAyahsReadResult.DoorNotFound();
        }

        if (door.IsArchived)
        {
            return new DoorLinkAyahsReadResult.DoorArchived();
        }

        return expectedDoorVersion != door.Version
            ? new DoorLinkAyahsReadResult.DoorVersionStale()
            : null;
    }

    private sealed record DoorRow(uint Version, bool IsArchived);
    private sealed record UnitRow(long Id, bool IsGrouped);
    private sealed record UnitAyahRow(long UnitAyahId, int AyahId);
    private sealed record SelectedWordRow(long UnitAyahId, int QuranWordId);
    private sealed record DescriptionRow(long UnitAyahId, string Body);
    private sealed record RecordSummaryRow(
        long UnitId,
        bool IsGrouped,
        int AyahCount,
        int SelectedWordCount,
        int DescriptionCount,
        string FirstVerseKey,
        string LastVerseKey);
}
