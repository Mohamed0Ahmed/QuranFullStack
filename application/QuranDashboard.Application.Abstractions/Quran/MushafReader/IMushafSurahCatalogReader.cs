using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IMushafSurahCatalogReader
{
    Task<MushafSurahCatalogResponse> GetCatalogAsync(CancellationToken ct);
}
