using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;

public abstract record GetWordTypeTableOutcome
{
    private GetWordTypeTableOutcome() { }

    public sealed record Success(PagedResult<WordTypeTableRowDto> Page) : GetWordTypeTableOutcome;
    public sealed record InvalidTableView : GetWordTypeTableOutcome;
    public sealed record InvalidFilter : GetWordTypeTableOutcome;
    public sealed record InvalidSort : GetWordTypeTableOutcome;
    public sealed record InvalidPaging : GetWordTypeTableOutcome;
}
