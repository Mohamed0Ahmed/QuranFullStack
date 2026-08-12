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
            return new LinkingConfirmedDoorState(door.Id, door.Name, door.IsArchived, door.Version, []);
        }

        var contributionIds = contributions.Select(contribution => contribution.Id).ToList();

        var units = await db.LinkingUnits
            .AsNoTracking()
            .Where(unit => contributionIds.Contains(unit.SourceContributionId))
            .OrderBy(unit => unit.SourceContributionId)
            .ThenBy(unit => unit.OrderValue)
            .Select(unit => new UnitRow(unit.Id, unit.SourceContributionId, unit.OrderValue, unit.IsGrouped))
            .ToListAsync(cancellationToken);

        var unitAyahs = await (
            from unitAyah in db.LinkingUnitAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on unitAyah.AyahId equals ayah.Id
            where contributionIds.Contains(unitAyah.SourceContributionId)
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
            Assemble(contributions, units, unitAyahs, words, descriptions));
    }

    private static List<LinkingConfirmedContribution> Assemble(
        IReadOnlyList<ContributionRow> contributions,
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

        var unitsByContribution = units
            .GroupBy(unit => unit.SourceContributionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(unit => new LinkingConfirmedUnit(
                        unit.Id,
                        unit.OrderValue,
                        unit.IsGrouped,
                        ayahsByUnit.GetValueOrDefault(unit.Id, [])))
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

    private sealed record ContributionRow(
        long Id,
        uint Version,
        string SourceIdentity,
        LinkingSourceKind SourceKind,
        string Label,
        LinkingContributionMode ContributionMode,
        int OrderValue);

    private sealed record UnitRow(long Id, long SourceContributionId, int OrderValue, bool IsGrouped);

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
