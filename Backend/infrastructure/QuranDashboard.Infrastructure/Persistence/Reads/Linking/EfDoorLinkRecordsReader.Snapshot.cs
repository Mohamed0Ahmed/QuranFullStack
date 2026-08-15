using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

internal sealed partial class EfDoorLinkRecordsReader
{
    public async Task<DoorLinkSnapshotReadResult> ReadSnapshotAsync(
        int doorId,
        long linkingDataRevision,
        CancellationToken cancellationToken)
    {
        var door = await ReadDoorAsync(doorId, cancellationToken);
        if (door is null)
        {
            return new DoorLinkSnapshotReadResult.DoorNotFound();
        }
        if (door.IsArchived)
        {
            return new DoorLinkSnapshotReadResult.DoorArchived();
        }

        var units = await LiveUnits(doorId)
            .OrderBy(unit => unit.Id)
            .Select(unit => new SnapshotUnitRow(unit.Id, unit.IsGrouped))
            .ToListAsync(cancellationToken);
        var unitIds = units.Select(unit => unit.UnitId).ToList();
        var unitAyahs = await db.LinkingUnitAyahs.AsNoTracking()
            .Where(unitAyah => unitIds.Contains(unitAyah.UnitId))
            .OrderBy(unitAyah => unitAyah.UnitId)
            .ThenBy(unitAyah => unitAyah.OrderValue)
            .ThenBy(unitAyah => unitAyah.Id)
            .Select(unitAyah => new SnapshotUnitAyahRow(
                unitAyah.Id,
                unitAyah.UnitId,
                unitAyah.AyahId))
            .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(row => row.UnitAyahId).ToList();

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

        var selectedWords = await (
                from word in db.LinkingUnitAyahWords.AsNoTracking()
                join quranWord in db.QuranWords.AsNoTracking()
                    on word.QuranWordId equals quranWord.Id
                where unitAyahIds.Contains(word.UnitAyahId)
                orderby word.UnitAyahId, quranWord.WordNumber, quranWord.Id
                select new SnapshotSelectedWordRow(word.UnitAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);
        var selectedWordIdsByUnitAyah = selectedWords
            .GroupBy(row => row.UnitAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)[.. group.Select(row => row.QuranWordId)]);

        var descriptions = await db.LinkingUnitAyahDescriptions.AsNoTracking()
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .ThenBy(description => description.Id)
            .Select(description => new SnapshotDescriptionRow(
                description.UnitAyahId,
                description.Body))
            .ToListAsync(cancellationToken);
        var descriptionsByUnitAyah = descriptions
            .GroupBy(row => row.UnitAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group.Select(row => row.Body)]);

        var ayahIds = unitAyahs.Select(row => row.AyahId).Distinct().ToList();
        var ayahMeta = await LinkingAyahHydration.LoadByIdsAsync(db, ayahIds, cancellationToken);
        var hydratedAyahs = await LinkingAyahHydration.ProjectAsync(
            db,
            ayahMeta,
            new Dictionary<int, IReadOnlyList<int>>(),
            true,
            cancellationToken);
        var ayahs = hydratedAyahs
            .Select(ayah => new DoorLinkSnapshotAyahDto(
                ayah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                ayah.SurahNameArabic,
                ayah.PageFrom,
                ayah.PageTo,
                ayah.Words))
            .ToList();

        var unitAyahsByUnit = unitAyahs
            .GroupBy(row => row.UnitId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DoorLinkSnapshotRecordAyahDto>)[.. group.Select(row =>
                    new DoorLinkSnapshotRecordAyahDto(
                        row.AyahId,
                        selectedWordIdsByUnitAyah.GetValueOrDefault(row.UnitAyahId, []),
                        descriptionsByUnitAyah.GetValueOrDefault(row.UnitAyahId, [])))]);
        var records = units
            .Select(unit => new DoorLinkSnapshotRecordDto(
                unit.UnitId,
                unit.IsGrouped,
                sourceLabelsByUnit.GetValueOrDefault(unit.UnitId, []),
                unitAyahsByUnit.GetValueOrDefault(unit.UnitId, [])))
            .ToList();

        return new DoorLinkSnapshotReadResult.Success(new DoorLinkSnapshotDto(
            doorId,
            door.Version,
            linkingDataRevision,
            records,
            ayahs));
    }

    private sealed record SnapshotUnitRow(long UnitId, bool IsGrouped);
    private sealed record SnapshotUnitAyahRow(long UnitAyahId, long UnitId, int AyahId);
    private sealed record SnapshotSelectedWordRow(long UnitAyahId, int QuranWordId);
    private sealed record SnapshotDescriptionRow(long UnitAyahId, string Body);
}
