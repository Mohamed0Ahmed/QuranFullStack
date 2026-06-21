using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IAyahMutashabihatReader
{
    Task<AyahMutashabihatResponse?> GetAyahMutashabihatAsync(string verseKey, CancellationToken ct);
}
