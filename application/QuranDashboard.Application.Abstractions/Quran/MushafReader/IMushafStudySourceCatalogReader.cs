using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Read-only catalog of all tafsir, translation, and full-i3rab sources for
/// the ayah-study source selectors.
/// </summary>
public interface IMushafStudySourceCatalogReader
{
    Task<MushafStudySourceCatalogResponse> GetCatalogAsync(CancellationToken ct);
}
