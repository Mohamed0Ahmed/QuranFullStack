using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfMushafStudySourceCatalogReader(QuranDashboardDbContext db) : IMushafStudySourceCatalogReader
{
    public async Task<MushafStudySourceCatalogResponse> GetCatalogAsync(CancellationToken ct)
    {
        var tafsirSources = await db.TafsirSources
            .AsNoTracking()
            .OrderBy(s => s.LanguageNameAr)
            .ThenBy(s => s.TafsirKind)
            .ThenBy(s => s.DisplayNameAr)
            .Select(s => new StudySourceCatalogItem(
                s.SourceKey,
                s.DisplayNameAr,
                s.DisplayNameEn,
                s.LanguageCode,
                s.LanguageNameAr,
                s.Direction,
                s.TafsirKind,
                null))
            .ToListAsync(ct);

        var translationSources = await db.TranslationSources
            .AsNoTracking()
            .OrderBy(s => s.LanguageNameAr)
            .ThenBy(s => s.TranslationType)
            .ThenBy(s => s.DisplayNameAr)
            .Select(s => new StudySourceCatalogItem(
                s.SourceKey,
                s.DisplayNameAr,
                s.DisplayNameEn,
                s.LanguageCode,
                s.LanguageNameAr,
                s.Direction,
                null,
                s.TranslationType))
            .ToListAsync(ct);

        var fullI3rabSources = await db.FullI3rabSources
            .AsNoTracking()
            .OrderBy(s => s.DisplayNameAr)
            .Select(s => new StudySourceCatalogItem(
                s.SourceKey,
                s.DisplayNameAr,
                s.DisplayNameEn,
                s.LanguageCode,
                null,
                s.Direction,
                null,
                null))
            .ToListAsync(ct);

        return new MushafStudySourceCatalogResponse(
            tafsirSources,
            translationSources,
            fullI3rabSources);
    }
}
