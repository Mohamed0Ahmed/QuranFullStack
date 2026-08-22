using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

internal sealed class EfMushafAyahDoorsReader(QuranDashboardDbContext db) : IMushafAyahDoorsReader
{
    public async Task<MushafAyahDoorsResponse?> GetDoorsAsync(string verseKey, CancellationToken ct)
    {
        var ayahId = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => ayah.VerseKey == verseKey)
            .Select(ayah => (int?)ayah.Id)
            .SingleOrDefaultAsync(ct);

        if (ayahId is null)
        {
            return null;
        }

        var doorIds = await (
            from doorAyah in db.LinkingDoorAyahs.AsNoTracking()
            join door in db.AbwabDoors.AsNoTracking() on doorAyah.DoorId equals door.Id
            where doorAyah.AyahId == ayahId.Value && door.DeletedAtUtc == null
            select door.Id)
            .Distinct()
            .OrderBy(doorId => doorId)
            .ToListAsync(ct);

        return new MushafAyahDoorsResponse(verseKey, doorIds);
    }
}
