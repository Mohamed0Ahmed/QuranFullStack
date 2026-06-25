using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

/// <summary>
/// EF Core read model for the Lemmas Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. Method bodies are implemented in the lemma
/// catalogue and detail story phases (T032/T033/T054/T066/T078/T088); this
/// skeleton exists so the bounded cache decorator and DI wiring can compile and
/// be exercised by the Feature 016 test fixture.
/// </summary>
public sealed class EfLemmasReader(QuranDashboardDbContext db) : ILemmasReader
{
    private readonly QuranDashboardDbContext _db = db;

    public Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaSummaryDto?> GetLemmaSummaryAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
        int id,
        LemmaWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaStemsResponse?> GetLemmaStemsAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
