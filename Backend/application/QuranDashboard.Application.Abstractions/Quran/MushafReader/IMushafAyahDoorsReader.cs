using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public interface IMushafAyahDoorsReader
{
    Task<MushafAyahDoorsResponse?> GetDoorsAsync(string verseKey, CancellationToken ct);
}
