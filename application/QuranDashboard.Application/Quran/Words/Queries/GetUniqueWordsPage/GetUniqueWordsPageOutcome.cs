using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

/// <summary>
/// Discriminated outcome for a Unique Words list read. Mirrors the project's
/// outcome pattern: a closed set of variants the controller maps to
/// <c>ApiResponse&lt;T&gt;</c> with <c>200</c>/<c>400</c>.
/// </summary>
/// <remarks>
/// There is intentionally no <c>NotFound</c> variant: a list page always exists.
/// An empty result is <see cref="Success"/> with <c>TotalCount = 0</c> and empty
/// <see cref="PagedResult{T}.Items"/>.
/// </remarks>
public abstract record GetUniqueWordsPageOutcome
{
    private GetUniqueWordsPageOutcome() { }

    public sealed record Success(PagedResult<UniqueWordListItemDto> Page) : GetUniqueWordsPageOutcome;
    public sealed record InvalidKind : GetUniqueWordsPageOutcome;
    public sealed record InvalidSort : GetUniqueWordsPageOutcome;
    public sealed record InvalidPaging : GetUniqueWordsPageOutcome;
}
