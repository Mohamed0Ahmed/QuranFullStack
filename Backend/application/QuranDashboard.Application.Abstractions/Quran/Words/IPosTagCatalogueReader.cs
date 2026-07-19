namespace QuranDashboard.Application.Abstractions.Quran.Words;

// Guards the Unique Words primaryType association filter: an unknown POS code is rejected with a
// controlled 400 rather than silently matching nothing.
public interface IPosTagCatalogueReader
{
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken);
}
