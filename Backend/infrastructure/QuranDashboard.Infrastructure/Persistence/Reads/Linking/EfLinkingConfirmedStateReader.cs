using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

internal sealed class EfLinkingConfirmedStateReader(QuranDashboardDbContext db) : ILinkingConfirmedStateReader
{
    public async Task<LinkingConfirmedDoorState?> LoadAsync(int doorId, CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors
            .AsNoTracking()
            .Where(candidate => candidate.Id == doorId)
            .Select(candidate => new DoorRow(
                candidate.Id, candidate.Name, candidate.DeletedAtUtc != null, candidate.Version))
            .FirstOrDefaultAsync(cancellationToken);

        if (door is null)
        {
            return null;
        }

        var doorAyahs = await (
            from doorAyah in db.LinkingDoorAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on doorAyah.AyahId equals ayah.Id
            where doorAyah.DoorId == doorId
            orderby ayah.SurahNumber, ayah.AyahNumber
            select new DoorAyahRow(
                doorAyah.Id,
                doorAyah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber))
            .ToListAsync(cancellationToken);
        var doorAyahIds = doorAyahs.Select(ayah => ayah.Id).ToList();
        var doorWords = doorAyahIds.Count == 0
            ? []
            : await db.LinkingDoorAyahWords
                .AsNoTracking()
                .Where(word => doorAyahIds.Contains(word.DoorAyahId))
                .OrderBy(word => word.DoorAyahId)
                .ThenBy(word => word.QuranWordId)
                .Select(word => new DoorWordRow(word.DoorAyahId, word.QuranWordId))
                .ToListAsync(cancellationToken);
        var confirmedDoorAyahs = AssembleDoorAyahs(doorAyahs, doorWords);

        var contributions = await db.LinkingSourceContributions
            .AsNoTracking()
            .Where(contribution => contribution.DoorId == doorId && contribution.DeletedAtUtc == null)
            .OrderBy(contribution => contribution.OrderValue)
            .ThenBy(contribution => contribution.Id)
            .Select(contribution => new ContributionRow(
                contribution.Id,
                contribution.Version,
                contribution.SourceIdentity,
                contribution.SourceKind,
                contribution.Label,
                contribution.ContributionMode,
                contribution.OrderValue))
            .ToListAsync(cancellationToken);

        if (contributions.Count == 0)
        {
            return new LinkingConfirmedDoorState(
                door.Id, door.Name, door.IsArchived, door.Version, confirmedDoorAyahs, []);
        }

        var contributionIds = contributions.Select(contribution => contribution.Id).ToList();
        var contributionUnits = await db.LinkingSourceContributionUnits
            .AsNoTracking()
            .Where(link => contributionIds.Contains(link.SourceContributionId))
            .OrderBy(link => link.SourceContributionId)
            .ThenBy(link => link.OrderValue)
            .Select(link => new ContributionUnitRow(link.SourceContributionId, link.UnitId, link.OrderValue))
            .ToListAsync(cancellationToken);
        var unitIds = contributionUnits.Select(link => link.UnitId).Distinct().ToList();

        var units = await db.LinkingUnits
            .AsNoTracking()
            .Where(unit => unitIds.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .Select(unit => new UnitRow(unit.Id, unit.Identity, unit.IsGrouped))
            .ToListAsync(cancellationToken);

        var unitAyahs = await (
            from unitAyah in db.LinkingUnitAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on unitAyah.AyahId equals ayah.Id
            where unitIds.Contains(unitAyah.UnitId)
            orderby unitAyah.UnitId, unitAyah.OrderValue, unitAyah.Id
            select new UnitAyahRow(
                unitAyah.Id,
                unitAyah.UnitId,
                unitAyah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                unitAyah.OrderValue))
            .ToListAsync(cancellationToken);

        var unitAyahIds = unitAyahs.Select(unitAyah => unitAyah.Id).ToList();

        var words = await db.LinkingUnitAyahWords
            .AsNoTracking()
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .OrderBy(word => word.UnitAyahId)
            .ThenBy(word => word.QuranWordId)
            .Select(word => new WordRow(word.UnitAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);

        var descriptions = await db.LinkingUnitAyahDescriptions
            .AsNoTracking()
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .Select(description => new DescriptionRow(description.UnitAyahId, description.Body))
            .ToListAsync(cancellationToken);

        return new LinkingConfirmedDoorState(
            door.Id,
            door.Name,
            door.IsArchived,
            door.Version,
            confirmedDoorAyahs,
            Assemble(contributions, contributionUnits, units, unitAyahs, words, descriptions));
    }

    public async Task<LinkingConfirmedDoorState?> LoadAffectedAsync(
        int doorId,
        IReadOnlyList<string> requestedContributionIdentities,
        IReadOnlyList<int> requestedAyahIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedContributionIdentities);
        ArgumentNullException.ThrowIfNull(requestedAyahIds);

        var door = await db.AbwabDoors
            .AsNoTracking()
            .Where(candidate => candidate.Id == doorId)
            .Select(candidate => new DoorRow(
                candidate.Id,
                candidate.Name,
                candidate.DeletedAtUtc != null,
                candidate.Version))
            .FirstOrDefaultAsync(cancellationToken);
        if (door is null)
        {
            return null;
        }

        var requestedContributions = await db.LinkingSourceContributions
            .AsNoTracking()
            .Where(contribution =>
                contribution.DoorId == doorId
                && contribution.DeletedAtUtc == null
                && requestedContributionIdentities.Contains(contribution.SourceIdentity))
            .Select(contribution => contribution.Id)
            .ToListAsync(cancellationToken);
        var oldAyahIds = requestedContributions.Count == 0
            ? []
            : await (
                from link in db.LinkingSourceContributionUnits.AsNoTracking()
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking() on link.UnitId equals unitAyah.UnitId
                where requestedContributions.Contains(link.SourceContributionId)
                select unitAyah.AyahId)
                .Distinct()
                .ToListAsync(cancellationToken);
        var affectedAyahIds = requestedAyahIds.Concat(oldAyahIds).Distinct().ToList();

        var intersectingContributionIds = affectedAyahIds.Count == 0
            ? []
            : await (
                from contribution in db.LinkingSourceContributions.AsNoTracking()
                join link in db.LinkingSourceContributionUnits.AsNoTracking()
                    on contribution.Id equals link.SourceContributionId
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking() on link.UnitId equals unitAyah.UnitId
                where contribution.DoorId == doorId
                    && contribution.DeletedAtUtc == null
                    && affectedAyahIds.Contains(unitAyah.AyahId)
                select contribution.Id)
                .Distinct()
                .ToListAsync(cancellationToken);
        var contributionIds = requestedContributions
            .Concat(intersectingContributionIds)
            .Distinct()
            .ToList();
        var contributions = await db.LinkingSourceContributions
            .AsNoTracking()
            .Where(contribution => contributionIds.Contains(contribution.Id))
            .OrderBy(contribution => contribution.OrderValue)
            .ThenBy(contribution => contribution.Id)
            .Select(contribution => new ContributionRow(
                contribution.Id,
                contribution.Version,
                contribution.SourceIdentity,
                contribution.SourceKind,
                contribution.Label,
                contribution.ContributionMode,
                contribution.OrderValue))
            .ToListAsync(cancellationToken);
        var requestedContributionUnits = await db.LinkingSourceContributionUnits
            .AsNoTracking()
            .Where(link => requestedContributions.Contains(link.SourceContributionId))
            .OrderBy(link => link.SourceContributionId)
            .ThenBy(link => link.OrderValue)
            .Select(link => new ContributionUnitRow(
                link.SourceContributionId,
                link.UnitId,
                link.OrderValue))
            .ToListAsync(cancellationToken);
        var otherContributionIds = contributionIds.Except(requestedContributions).ToList();
        var otherContributionUnits = affectedAyahIds.Count == 0 || otherContributionIds.Count == 0
            ? []
            : await (
                from link in db.LinkingSourceContributionUnits.AsNoTracking()
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking() on link.UnitId equals unitAyah.UnitId
                where otherContributionIds.Contains(link.SourceContributionId)
                    && affectedAyahIds.Contains(unitAyah.AyahId)
                orderby link.SourceContributionId, link.OrderValue
                select new ContributionUnitRow(
                    link.SourceContributionId,
                    link.UnitId,
                    link.OrderValue))
                .Distinct()
                .ToListAsync(cancellationToken);
        var contributionUnits = requestedContributionUnits
            .Concat(otherContributionUnits)
            .OrderBy(link => link.SourceContributionId)
            .ThenBy(link => link.OrderValue)
            .ToList();
        var unitIds = contributionUnits.Select(link => link.UnitId).Distinct().ToList();
        var requestedUnitIds = requestedContributionUnits.Select(link => link.UnitId).Distinct().ToList();
        var otherUnitIds = otherContributionUnits.Select(link => link.UnitId).Distinct().ToList();
        var units = await db.LinkingUnits
            .AsNoTracking()
            .Where(unit => unitIds.Contains(unit.Id))
            .Select(unit => new UnitRow(unit.Id, unit.Identity, unit.IsGrouped))
            .ToListAsync(cancellationToken);
        var unitAyahs = await (
            from unitAyah in db.LinkingUnitAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on unitAyah.AyahId equals ayah.Id
            where requestedUnitIds.Contains(unitAyah.UnitId)
                || (otherUnitIds.Contains(unitAyah.UnitId) && affectedAyahIds.Contains(unitAyah.AyahId))
            orderby unitAyah.UnitId, unitAyah.OrderValue, unitAyah.Id
            select new UnitAyahRow(
                unitAyah.Id,
                unitAyah.UnitId,
                unitAyah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                unitAyah.OrderValue))
            .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(ayah => ayah.Id).ToList();
        var words = await db.LinkingUnitAyahWords
            .AsNoTracking()
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .OrderBy(word => word.UnitAyahId)
            .ThenBy(word => word.QuranWordId)
            .Select(word => new WordRow(word.UnitAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingUnitAyahDescriptions
            .AsNoTracking()
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .Select(description => new DescriptionRow(description.UnitAyahId, description.Body))
            .ToListAsync(cancellationToken);

        var doorAyahs = await (
            from doorAyah in db.LinkingDoorAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on doorAyah.AyahId equals ayah.Id
            where doorAyah.DoorId == doorId && affectedAyahIds.Contains(doorAyah.AyahId)
            orderby ayah.SurahNumber, ayah.AyahNumber
            select new DoorAyahRow(
                doorAyah.Id,
                doorAyah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber))
            .ToListAsync(cancellationToken);
        var doorAyahRowIds = doorAyahs.Select(ayah => ayah.Id).ToList();
        var doorWords = await db.LinkingDoorAyahWords
            .AsNoTracking()
            .Where(word => doorAyahRowIds.Contains(word.DoorAyahId))
            .OrderBy(word => word.DoorAyahId)
            .ThenBy(word => word.QuranWordId)
            .Select(word => new DoorWordRow(word.DoorAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);

        return new LinkingConfirmedDoorState(
            door.Id,
            door.Name,
            door.IsArchived,
            door.Version,
            AssembleDoorAyahs(doorAyahs, doorWords),
            Assemble(contributions, contributionUnits, units, unitAyahs, words, descriptions));
    }

    private static List<LinkingConfirmedDoorAyah> AssembleDoorAyahs(
        IReadOnlyList<DoorAyahRow> ayahs,
        IReadOnlyList<DoorWordRow> words)
    {
        var wordsByAyah = words
            .GroupBy(word => word.DoorAyahId)
            .ToDictionary(group => group.Key, group => group.Select(word => word.QuranWordId).ToList());

        return
        [
            .. ayahs.Select(ayah => new LinkingConfirmedDoorAyah(
                ayah.Id,
                ayah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                wordsByAyah.GetValueOrDefault(ayah.Id, [])))
        ];
    }

    private static List<LinkingConfirmedContribution> Assemble(
        IReadOnlyList<ContributionRow> contributions,
        IReadOnlyList<ContributionUnitRow> contributionUnits,
        IReadOnlyList<UnitRow> units,
        IReadOnlyList<UnitAyahRow> unitAyahs,
        IReadOnlyList<WordRow> words,
        IReadOnlyList<DescriptionRow> descriptions)
    {
        var wordsByUnitAyah = words
            .GroupBy(word => word.UnitAyahId)
            .ToDictionary(group => group.Key, group => group.Select(word => word.QuranWordId).ToList());

        var descriptionsByUnitAyah = descriptions
            .GroupBy(description => description.UnitAyahId)
            .ToDictionary(group => group.Key, group => group.Select(description => description.Body).ToList());

        var ayahsByUnit = unitAyahs
            .GroupBy(unitAyah => unitAyah.UnitId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(unitAyah => new LinkingConfirmedAyah(
                        unitAyah.Id,
                        unitAyah.AyahId,
                        unitAyah.VerseKey,
                        unitAyah.SurahNumber,
                        unitAyah.AyahNumber,
                        unitAyah.OrderValue,
                        wordsByUnitAyah.GetValueOrDefault(unitAyah.Id, []),
                        descriptionsByUnitAyah.GetValueOrDefault(unitAyah.Id, [])))
                    .ToList());

        var unitsById = units.ToDictionary(unit => unit.Id);
        var unitsByContribution = contributionUnits
            .GroupBy(link => link.SourceContributionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(link =>
                    {
                        var unit = unitsById[link.UnitId];

                        return new LinkingConfirmedUnit(
                        unit.Id,
                        unit.Identity,
                        link.OrderValue,
                        unit.IsGrouped,
                        ayahsByUnit.GetValueOrDefault(unit.Id, []));
                    })
                    .ToList());

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

    private sealed record DoorRow(int Id, string Name, bool IsArchived, uint Version);

    private sealed record DoorAyahRow(
        long Id,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber);

    private sealed record DoorWordRow(long DoorAyahId, int QuranWordId);

    private sealed record ContributionRow(
        long Id,
        uint Version,
        string SourceIdentity,
        LinkingSourceKind SourceKind,
        string Label,
        LinkingContributionMode ContributionMode,
        int OrderValue);

    private sealed record ContributionUnitRow(long SourceContributionId, long UnitId, int OrderValue);

    private sealed record UnitRow(long Id, string Identity, bool IsGrouped);

    private sealed record UnitAyahRow(
        long Id,
        long UnitId,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        int OrderValue);

    private sealed record WordRow(long UnitAyahId, int QuranWordId);

    private sealed record DescriptionRow(long UnitAyahId, string Body);
}
