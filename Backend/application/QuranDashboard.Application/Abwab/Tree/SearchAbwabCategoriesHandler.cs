using QuranDashboard.Application.Abstractions.Abwab.Core;

namespace QuranDashboard.Application.Abwab.Tree;

public sealed class SearchAbwabCategoriesHandler(IAbwabCoreReadPort readPort)
{
    public async Task<CategorySearchResultDto> HandleAsync(SearchAbwabCategoriesQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await readPort.SearchCategoriesAsync(query.Query, ct);
    }
}
