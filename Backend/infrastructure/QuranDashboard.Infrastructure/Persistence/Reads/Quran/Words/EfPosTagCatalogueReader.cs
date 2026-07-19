using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

public sealed class EfPosTagCatalogueReader(QuranDashboardDbContext db) : IPosTagCatalogueReader
{
    private readonly QuranDashboardDbContext _db = db;

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken) =>
        _db.PosTags.AsNoTracking().AnyAsync(pos => pos.Code == code, cancellationToken);
}
