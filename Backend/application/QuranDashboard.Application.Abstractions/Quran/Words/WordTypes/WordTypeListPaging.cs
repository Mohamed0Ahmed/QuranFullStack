namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeListPaging
{
    private const int MaxPageSize = 1000;

    private WordTypeListPaging(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }

    public static WordTypeListPaging? Create(int page, int pageSize) =>
        page < 1 || pageSize < 1 || pageSize > MaxPageSize ? null : new WordTypeListPaging(page, pageSize);
}
