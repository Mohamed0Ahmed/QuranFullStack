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
}
