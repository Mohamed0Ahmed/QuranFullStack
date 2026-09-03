using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

internal static class RootsWordsDerivation
{
    public static PagedResult<RootWordItemDto> ToPage(
        IReadOnlyList<RootWordItemDto> all,
        int page,
        int pageSize)
    {
        var totalCount = all.Count;
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<RootWordItemDto>(page, pageSize, totalCount, []);
        }

        var items = all
            .Skip(skip.Value)
            .Take(pageSize)
            .ToList();

        return new PagedResult<RootWordItemDto>(page, pageSize, totalCount, items);
    }
}
