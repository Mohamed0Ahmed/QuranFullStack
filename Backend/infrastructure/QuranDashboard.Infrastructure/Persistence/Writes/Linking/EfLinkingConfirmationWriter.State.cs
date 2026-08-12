using QuranDashboard.Application.Abstractions.Linking.Preflight;
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

        var contributions = await db.LinkingSourceContributions
            .Where(contribution => contribution.DoorId == doorId && contribution.DeletedAtUtc == null)
            .OrderBy(contribution => contribution.OrderValue)
            .ThenBy(contribution => contribution.Id)
            .ToListAsync(cancellationToken);

        if (contributions.Count == 0)
        {
            return new LockedConfirmationState(
                new LinkingConfirmedDoorState(
                    door.Id, door.Name, door.DeletedAtUtc is not null, door.Version, []),
                new Dictionary<long, LinkingSourceContribution>(),
                [],
                [],
                [],
                []);
        }

        var contributionIds = contributions.Select(contribution => contribution.Id).ToList();
        var units = await db.LinkingUnits
            .Where(unit => contributionIds.Contains(unit.SourceContributionId))
            .OrderBy(unit => unit.SourceContributionId)
            .ThenBy(unit => unit.OrderValue)
            .ToListAsync(cancellationToken);
        var unitAyahs = await db.LinkingUnitAyahs
            .Where(unitAyah => contributionIds.Contains(unitAyah.SourceContributionId))
            .OrderBy(unitAyah => unitAyah.UnitId)
            .ThenBy(unitAyah => unitAyah.OrderValue)
            .ThenBy(unitAyah => unitAyah.Id)
            .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(unitAyah => unitAyah.Id).ToList();
        var words = await db.LinkingUnitAyahWords
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .OrderBy(word => word.UnitAyahId)
            .ThenBy(word => word.QuranWordId)
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingUnitAyahDescriptions
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .ToListAsync(cancellationToken);
        var ayahIds = unitAyahs.Select(unitAyah => unitAyah.AyahId).Distinct().ToList();
        var ayahs = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => ayahIds.Contains(ayah.Id))
            .Select(ayah => new AyahRow(
                ayah.Id, ayah.VerseKey, ayah.SurahNumber, ayah.AyahNumber))
            .ToDictionaryAsync(ayah => ayah.Id, cancellationToken);

        return new LockedConfirmationState(
            new LinkingConfirmedDoorState(
                door.Id,
                door.Name,
                door.DeletedAtUtc is not null,
                door.Version,
                AssembleConfirmedState(contributions, units, unitAyahs, words, descriptions, ayahs)),
            contributions.ToDictionary(contribution => contribution.Id),
            units,
            unitAyahs,
            words,
            descriptions);
    }

    private static IReadOnlyList<LinkingConfirmedContribution> AssembleConfirmedState(
        IReadOnlyList<LinkingSourceContribution> contributions,
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
        var unitsByContribution = units
            .GroupBy(unit => unit.SourceContributionId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(unit => new LinkingConfirmedUnit(
                    unit.Id,
                    unit.OrderValue,
                    unit.IsGrouped,
                    ayahsByUnit.GetValueOrDefault(unit.Id, []))).ToList());

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
        LinkingConfirmedDoorState State,
        IReadOnlyDictionary<long, LinkingSourceContribution> ContributionsById,
        IReadOnlyList<LinkingUnit> Units,
        IReadOnlyList<LinkingUnitAyah> UnitAyahs,
        IReadOnlyList<LinkingUnitAyahWord> Words,
        IReadOnlyList<LinkingUnitAyahDescription> Descriptions);

    private sealed record AyahRow(int Id, string VerseKey, short SurahNumber, short AyahNumber);
}
