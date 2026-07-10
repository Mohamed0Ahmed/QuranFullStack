using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IMushafPageReader
{

    Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct);
}
