using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

/// <summary>
/// EF Core existence lookup over <c>quran_pos_tags</c> for the Unique Words association
/// filter validation (Feature 026, US7). Read-only, <c>AsNoTracking</c>; runs a single
/// bounded <c>EXISTS</c> only when a <c>primaryType</c> filter is supplied.
/// </summary>
public sealed class EfPosTagCatalogueReader(QuranDashboardDbContext db) : IPosTagCatalogueReader
{
    private readonly QuranDashboardDbContext _db = db;

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken) =>
        _db.PosTags.AsNoTracking().AnyAsync(pos => pos.Code == code, cancellationToken);
}
