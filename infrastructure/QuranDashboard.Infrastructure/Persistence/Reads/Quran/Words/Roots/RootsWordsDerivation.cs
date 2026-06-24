using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

internal static class RootsWordsDerivation
{
    public static PagedResult<RootWordItemDto> ToPage(
        IReadOnlyList<RootWordItemDto> all,
        int page,
        int pageSize)
    {
        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<RootWordItemDto>(page, pageSize, totalCount, items);
    }
}
