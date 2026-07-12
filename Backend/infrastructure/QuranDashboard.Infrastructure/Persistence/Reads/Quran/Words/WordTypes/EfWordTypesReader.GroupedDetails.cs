using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

// Grouped (root/stem/lemma) scoped detail reads. Kept in a dedicated partial so the primary reader
// stays under its size threshold. Every read reuses the same scoped BaseRowsSql occurrence base as the
// word/table rows and restricts to a single numeric dimension ID at head-word grain.
public sealed partial class EfWordTypesReader
{
    public async Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (!selection.IsValid)
        {
            return null;
        }

        var context = ToGroupedReadContext(selection.Filter);
        var parameters = BuildGroupedDetailParameters(context, selection.DimensionId);

        var row = await _dbContext.Database
            .SqlQueryRaw<GroupedSummarySqlResult>(GroupedSummarySql(context, selection.Kind), parameters)
            .SingleOrDefaultAsync(cancellationToken);

        return row?.ToDto(selection.Kind);
    }
}
