namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed record AbwabDoorInclusionSourceAyahSnapshot(
    int AyahId,
    IReadOnlyList<int> SelectedWordIds,
    IReadOnlyList<string> Descriptions);

internal sealed record AbwabDoorInclusionSourceSnapshot(
    long UnitId,
    bool IsGrouped,
    IReadOnlyList<AbwabDoorInclusionSourceAyahSnapshot> Ayahs)
{
    public static async Task<IReadOnlyDictionary<long, AbwabDoorInclusionSourceSnapshot>> LoadAsync(
        QuranDashboardDbContext db,
        IReadOnlyCollection<long> unitIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(unitIds);

        if (unitIds.Count == 0)
        {
            return new Dictionary<long, AbwabDoorInclusionSourceSnapshot>();
        }

        var ids = unitIds.Distinct().Order().ToArray();
        var units = await db.LinkingUnits.AsNoTracking()
            .Where(unit => ids.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .Select(unit => new { unit.Id, unit.IsGrouped })
            .ToListAsync(cancellationToken);

        var ayahs = await db.LinkingUnitAyahs.AsNoTracking()
            .Where(ayah => ids.Contains(ayah.UnitId))
            .OrderBy(ayah => ayah.UnitId)
            .ThenBy(ayah => ayah.AyahId)
            .ThenBy(ayah => ayah.OrderValue)
            .Select(ayah => new { ayah.Id, ayah.UnitId, ayah.AyahId })
            .ToListAsync(cancellationToken);

        var unitAyahIds = ayahs.Select(ayah => ayah.Id).ToArray();
        var words = await db.LinkingUnitAyahWords.AsNoTracking()
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .OrderBy(word => word.UnitAyahId)
            .ThenBy(word => word.QuranWordId)
            .Select(word => new { word.UnitAyahId, word.QuranWordId })
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingUnitAyahDescriptions.AsNoTracking()
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .OrderBy(description => description.UnitAyahId)
            .ThenBy(description => description.OrderValue)
            .ThenBy(description => description.Id)
            .Select(description => new { description.UnitAyahId, description.Body })
            .ToListAsync(cancellationToken);

        var wordsByAyah = words
            .GroupBy(word => word.UnitAyahId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<int>)group.Select(word => word.QuranWordId).ToArray());
        var descriptionsByAyah = descriptions
            .GroupBy(description => description.UnitAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(description => description.Body).ToArray());
        var ayahsByUnit = ayahs
            .GroupBy(ayah => ayah.UnitId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AbwabDoorInclusionSourceAyahSnapshot>)group
                    .Select(ayah => new AbwabDoorInclusionSourceAyahSnapshot(
                        ayah.AyahId,
                        wordsByAyah.GetValueOrDefault(ayah.Id, []),
                        descriptionsByAyah.GetValueOrDefault(ayah.Id, [])))
                    .ToArray());

        return units.ToDictionary(
            unit => unit.Id,
            unit => new AbwabDoorInclusionSourceSnapshot(
                unit.Id,
                unit.IsGrouped,
                ayahsByUnit.GetValueOrDefault(unit.Id, [])));
    }
}
