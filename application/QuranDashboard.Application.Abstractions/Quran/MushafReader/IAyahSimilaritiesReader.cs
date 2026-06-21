using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IAyahSimilaritiesReader
{
    Task<SimilarAyahsResponse?> GetSimilarAyahsAsync(string verseKey, CancellationToken ct);
}
