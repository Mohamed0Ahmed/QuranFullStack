using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed class EfWordTypesReader(QuranDashboardDbContext dbContext) : IWordTypesReader
{
    private readonly QuranDashboardDbContext _dbContext = dbContext;

    public Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        _ = _dbContext.QuranWords.AsNoTracking();
        throw new NotImplementedException("Word Types tree read is implemented in the story phase.");
    }

    public Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = _dbContext.QuranWords.AsNoTracking();
        throw new NotImplementedException("Word Types rows read is implemented in the story phase.");
    }

    public Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        _ = _dbContext.QuranWords.AsNoTracking();
        throw new NotImplementedException("Word Types summary read is implemented in the story phase.");
    }

    public Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = _dbContext.QuranWords.AsNoTracking();
        throw new NotImplementedException("Word Types ayah read is implemented in the story phase.");
    }

    public Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        _ = _dbContext.QuranWords.AsNoTracking();
        throw new NotImplementedException("Word Types surah read is implemented in the story phase.");
    }
}
