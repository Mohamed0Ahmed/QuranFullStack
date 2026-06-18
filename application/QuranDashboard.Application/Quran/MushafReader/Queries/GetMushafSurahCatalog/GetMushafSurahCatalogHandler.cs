using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafSurahCatalog;

public sealed class GetMushafSurahCatalogHandler(IMushafSurahCatalogReader catalogReader)
{
    public async Task<MushafSurahCatalogResponse> HandleAsync(
        GetMushafSurahCatalogQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await catalogReader.GetCatalogAsync(ct);
    }
}
