namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeDetailPaging
{
    private const int MaxPageSize = 100;

    private WordTypeDetailPaging(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }

    public static WordTypeDetailPaging? Create(int page, int pageSize) =>
        page < 1 || pageSize < 1 || pageSize > MaxPageSize ? null : new WordTypeDetailPaging(page, pageSize);
}
