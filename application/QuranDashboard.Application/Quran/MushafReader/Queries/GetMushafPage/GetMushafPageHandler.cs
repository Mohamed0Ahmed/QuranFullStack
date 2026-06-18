using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

public sealed class GetMushafPageHandler(IMushafPageReader pageReader)
{
    private const int MinPageNumber = 1;
    private const int MaxPageNumber = 604;

    public async Task<GetMushafPageOutcome> HandleAsync(GetMushafPageQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PageNumber < MinPageNumber || query.PageNumber > MaxPageNumber)
        {
            return new GetMushafPageOutcome.InvalidPageNumber();
        }

        var page = await pageReader.GetPageAsync(query.PageNumber, ct);
        if (page is null)
        {
            return new GetMushafPageOutcome.NotFound();
        }

        var response = page with
        {
            PreviousPageNumber = query.PageNumber > MinPageNumber ? query.PageNumber - 1 : null,
            NextPageNumber = query.PageNumber < MaxPageNumber ? query.PageNumber + 1 : null,
        };

        return new GetMushafPageOutcome.Success(response);
    }
}
