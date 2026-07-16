namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

internal static class ReadPaging
{
    internal static int? CalculateSafeSkip(int page, int pageSize, int totalCount)
    {
        var skip = ((long)page - 1) * pageSize;
        if (skip >= totalCount || skip > int.MaxValue)
        {
            return null;
        }

        return (int)skip;
    }

    // Window-count paging (B1): compute the page offset WITHOUT a preceding COUNT command. The page query
    // projects COUNT(*) OVER() to carry the scope total, so the total is not known up front. A null result
    // means the offset cannot address any page (non-positive page or int overflow) — an out-of-range empty
    // page whose TotalCount the caller recovers with the count-only fallback.
    internal static int? CalculatePageOffset(int page, int pageSize)
    {
        var skip = ((long)page - 1) * pageSize;
        if (skip < 0 || skip > int.MaxValue)
        {
            return null;
        }

        return (int)skip;
    }
}
