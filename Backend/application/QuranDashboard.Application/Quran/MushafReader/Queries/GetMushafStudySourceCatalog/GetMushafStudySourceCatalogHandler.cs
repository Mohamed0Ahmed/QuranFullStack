using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafStudySourceCatalog;

public sealed class GetMushafStudySourceCatalogHandler(IMushafStudySourceCatalogReader catalogReader)
{
    public async Task<MushafStudySourceCatalogResponse> HandleAsync(
        GetMushafStudySourceCatalogQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await catalogReader.GetCatalogAsync(ct);
    }
}
