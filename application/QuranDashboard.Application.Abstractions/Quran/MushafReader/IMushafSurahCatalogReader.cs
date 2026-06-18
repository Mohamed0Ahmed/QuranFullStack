using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Read-only surah catalog for jump-by-surah navigation (surah number → start page).
/// </summary>
public interface IMushafSurahCatalogReader
{
    Task<MushafSurahCatalogResponse> GetCatalogAsync(CancellationToken ct);
}
