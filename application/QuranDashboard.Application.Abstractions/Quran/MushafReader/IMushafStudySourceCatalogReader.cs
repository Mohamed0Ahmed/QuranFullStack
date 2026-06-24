using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IMushafStudySourceCatalogReader
{
    Task<MushafStudySourceCatalogResponse> GetCatalogAsync(CancellationToken ct);
}
