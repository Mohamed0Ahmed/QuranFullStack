using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

/// <summary>
/// EF Core read service for the Roots Explorer (Feature 015). Read-only:
/// <c>AsNoTracking</c>, no writes, no migrations. Mirrors Feature 014
/// <c>EfUniqueWordsReader</c>.
/// </summary>
/// <remarks>
/// This is the foundational skeleton: every method throws
/// <see cref="NotImplementedException"/> until its owning user story wires the
/// real aggregation (T022/T023 list+summary, T035 ayahs, T044 words,
/// T053 surahs, T061 lemmas/stems).
/// </remarks>
public sealed class EfRootsReader(QuranDashboardDbContext db) : IRootsReader
{
    private readonly QuranDashboardDbContext _db = db;

    /// <inheritdoc />
    public Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
