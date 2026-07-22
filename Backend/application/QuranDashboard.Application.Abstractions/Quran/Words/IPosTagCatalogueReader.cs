namespace QuranDashboard.Application.Abstractions.Quran.Words;

public interface IPosTagCatalogueReader
{
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken);
}
