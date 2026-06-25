using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

/// <summary>
/// EF Core read model for the Stems Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. Method bodies are implemented in the stem
/// catalogue and detail story phases (T043/T044/T055/T067/T079/T089); this
/// skeleton exists so the bounded cache decorator and DI wiring can compile and
/// be exercised by the Feature 016 test fixture.
/// </summary>
public sealed class EfStemsReader(QuranDashboardDbContext db) : IStemsReader
{
    private readonly QuranDashboardDbContext _db = db;

    public Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StemSummaryDto?> GetStemSummaryAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<PagedResult<StemWordItemDto>?> GetStemWordsAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<StemLemmasResponse?> GetStemLemmasAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
