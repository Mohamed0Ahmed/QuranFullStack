using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<LockedConfirmationState?> LoadLockedStateAsync(
        int doorId,
        CancellationToken cancellationToken)
    {
        var doors = await db.AbwabDoors
            .FromSqlInterpolated(
                $"""
                SELECT id, section_id, parent_id, name, description, representative_ayah_text,
                       order_value, global_order_value, created_at, created_by, updated_at, updated_by,
                       approved_at, approved_by, deleted_at, deleted_by, xmin
                FROM abwab_doors
                WHERE id = {doorId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var door = doors.SingleOrDefault();

        if (door is null)
        {
            return null;
        }

        var doorAyahs = await db.LinkingDoorAyahs
            .AsNoTracking()
            .Where(ayah => ayah.DoorId == doorId)
            .OrderBy(ayah => ayah.AyahId)
            .ToListAsync(cancellationToken);
        var doorAyahIds = doorAyahs.Select(ayah => ayah.Id).ToList();
        var doorWords = doorAyahIds.Count == 0
            ? []
            : await db.LinkingDoorAyahWords
                .AsNoTracking()
                .Where(word => doorAyahIds.Contains(word.DoorAyahId))
                .OrderBy(word => word.DoorAyahId)
                .ThenBy(word => word.QuranWordId)
                .ToListAsync(cancellationToken);
        var contributions = await db.LinkingSourceContributions
            .AsNoTracking()
            .Where(contribution => contribution.DoorId == doorId && contribution.DeletedAtUtc == null)
            .OrderBy(contribution => contribution.OrderValue)
            .ThenBy(contribution => contribution.Id)
            .ToListAsync(cancellationToken);
        var contributionIds = contributions.Select(contribution => contribution.Id).ToList();
        var contributionUnits = contributionIds.Count == 0
            ? []
            : await db.LinkingSourceContributionUnits
                .AsNoTracking()
                .Where(link => contributionIds.Contains(link.SourceContributionId))
                .OrderBy(link => link.SourceContributionId)
                .ThenBy(link => link.OrderValue)
                .ToListAsync(cancellationToken);
        var units = await db.LinkingUnits
            .AsNoTracking()
            .Where(unit => unit.DoorId == doorId)
            .OrderBy(unit => unit.Id)
            .ToListAsync(cancellationToken);
        var unitIds = units.Select(unit => unit.Id).ToList();
        var unitAyahs = unitIds.Count == 0
            ? []
            : await db.LinkingUnitAyahs
                .AsNoTracking()
                .Where(unitAyah => unitIds.Contains(unitAyah.UnitId))
                .OrderBy(unitAyah => unitAyah.UnitId)
                .ThenBy(unitAyah => unitAyah.OrderValue)
                .ThenBy(unitAyah => unitAyah.Id)
                .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(unitAyah => unitAyah.Id).ToList();
        var words = unitAyahIds.Count == 0
            ? []
            : await db.LinkingUnitAyahWords
                .AsNoTracking()
                .Where(word => unitAyahIds.Contains(word.UnitAyahId))
                .OrderBy(word => word.UnitAyahId)
                .ThenBy(word => word.QuranWordId)
                .ToListAsync(cancellationToken);
        var descriptions = unitAyahIds.Count == 0
            ? []
            : await db.LinkingUnitAyahDescriptions
                .AsNoTracking()
                .Where(description => unitAyahIds.Contains(description.UnitAyahId))
                .OrderBy(description => description.UnitAyahId)
                .ThenBy(description => description.OrderValue)
                .ToListAsync(cancellationToken);
        var ayahIds = doorAyahs.Select(ayah => ayah.AyahId)
            .Concat(unitAyahs.Select(ayah => ayah.AyahId))
            .Distinct()
            .ToList();
        var ayahs = ayahIds.Count == 0
            ? []
            : await db.QuranAyahs
                .AsNoTracking()
                .Where(ayah => ayahIds.Contains(ayah.Id))
                .Select(ayah => new AyahRow(
                    ayah.Id, ayah.VerseKey, ayah.SurahNumber, ayah.AyahNumber))
                .ToDictionaryAsync(ayah => ayah.Id, cancellationToken);
        var state = new LinkingConfirmedDoorState(
            door.Id,
            door.Name,
            door.DeletedAtUtc is not null,
            door.Version,
            AssembleDoorState(doorAyahs, doorWords, ayahs),
            AssembleContributionState(
                contributions,
                contributionUnits,
                units,
                unitAyahs,
                words,
                descriptions,
                ayahs));

        return new LockedConfirmationState(
            door,
            state,
            doorAyahs,
            doorWords,
            contributions.ToDictionary(contribution => contribution.Id),
            contributionUnits,
            units,
            unitAyahs,
            words,
            descriptions);
    }

    private static IReadOnlyList<LinkingConfirmedDoorAyah> AssembleDoorState(
        IReadOnlyList<LinkingDoorAyah> doorAyahs,
        IReadOnlyList<LinkingDoorAyahWord> words,
        IReadOnlyDictionary<int, AyahRow> ayahs)
    {
        var wordsByAyah = words
            .GroupBy(word => word.DoorAyahId)
            .ToDictionary(group => group.Key, group => group.Select(word => word.QuranWordId).ToList());

        return
        [
            .. doorAyahs.Select(doorAyah =>
            {
                var ayah = ayahs[doorAyah.AyahId];

                return new LinkingConfirmedDoorAyah(
                    doorAyah.Id,
                    doorAyah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    wordsByAyah.GetValueOrDefault(doorAyah.Id, []));
            })
        ];
    }

    private static IReadOnlyList<LinkingConfirmedContribution> AssembleContributionState(
        IReadOnlyList<LinkingSourceContribution> contributions,
        IReadOnlyList<LinkingSourceContributionUnit> contributionUnits,
        IReadOnlyList<LinkingUnit> units,
        IReadOnlyList<LinkingUnitAyah> unitAyahs,
        IReadOnlyList<LinkingUnitAyahWord> words,
        IReadOnlyList<LinkingUnitAyahDescription> descriptions,
        IReadOnlyDictionary<int, AyahRow> ayahs)
    {
        var wordsByAyah = words
            .GroupBy(word => word.UnitAyahId)
            .ToDictionary(group => group.Key, group => group.Select(word => word.QuranWordId).ToList());
        var descriptionsByAyah = descriptions
            .GroupBy(description => description.UnitAyahId)
            .ToDictionary(group => group.Key, group => group.Select(description => description.Body).ToList());
        var ayahsByUnit = unitAyahs
            .GroupBy(unitAyah => unitAyah.UnitId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(unitAyah =>
                {
                    var ayah = ayahs[unitAyah.AyahId];

                    return new LinkingConfirmedAyah(
                        unitAyah.Id,
                        unitAyah.AyahId,
                        ayah.VerseKey,
                        ayah.SurahNumber,
                        ayah.AyahNumber,
                        unitAyah.OrderValue,
                        wordsByAyah.GetValueOrDefault(unitAyah.Id, []),
                        descriptionsByAyah.GetValueOrDefault(unitAyah.Id, []));
                }).ToList());
        var unitsById = units.ToDictionary(unit => unit.Id);
        var unitsByContribution = contributionUnits
            .GroupBy(link => link.SourceContributionId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link =>
                {
                    var unit = unitsById[link.UnitId];

                    return new LinkingConfirmedUnit(
                    unit.Id,
                    unit.Identity,
                    link.OrderValue,
                    unit.IsGrouped,
                    ayahsByUnit.GetValueOrDefault(unit.Id, []));
                }).ToList());

        return
        [
            .. contributions.Select(contribution => new LinkingConfirmedContribution(
                contribution.Id,
                contribution.Version,
                contribution.SourceIdentity,
                contribution.SourceKind,
                contribution.Label,
                contribution.ContributionMode,
                contribution.OrderValue,
                unitsByContribution.GetValueOrDefault(contribution.Id, [])))
        ];
    }

    private sealed record LockedConfirmationState(
        AbwabDoor Door,
        LinkingConfirmedDoorState State,
        IReadOnlyList<LinkingDoorAyah> DoorAyahs,
        IReadOnlyList<LinkingDoorAyahWord> DoorWords,
        IReadOnlyDictionary<long, LinkingSourceContribution> ContributionsById,
        IReadOnlyList<LinkingSourceContributionUnit> ContributionUnits,
        IReadOnlyList<LinkingUnit> Units,
        IReadOnlyList<LinkingUnitAyah> UnitAyahs,
        IReadOnlyList<LinkingUnitAyahWord> Words,
        IReadOnlyList<LinkingUnitAyahDescription> Descriptions);

    private sealed record AyahRow(int Id, string VerseKey, short SurahNumber, short AyahNumber);
}
