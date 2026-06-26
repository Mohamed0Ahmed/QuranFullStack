using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

internal static class MorphologyRelatedItemsOrdering
{
    internal static IReadOnlyList<LemmaStemItemDto> OrderLemmaStems(
        IEnumerable<(int StemId, string StemText, int QuranWordId)> rows)
    {
        return rows
            .GroupBy(row => row.StemId)
            .Select(group =>
            {
                var first = group.OrderBy(row => row.QuranWordId).First();
                return new
                {
                    StemId = group.Key,
                    Item = new LemmaStemItemDto(group.Key, first.StemText, group.Count()),
                    FirstQuranWordId = first.QuranWordId,
                };
            })
            .OrderByDescending(row => row.Item.OccurrencesCount)
            .ThenBy(row => row.FirstQuranWordId)
            .ThenBy(row => row.StemId)
            .Select(row => row.Item)
            .ToList();
    }

    internal static IReadOnlyList<StemLemmaItemDto> OrderStemLemmas(
        IEnumerable<(int LemmaId, string LemmaText, string? LemmaBuckwalter, int QuranWordId)> rows)
    {
        return rows
            .GroupBy(row => row.LemmaId)
            .Select(group =>
            {
                var first = group.OrderBy(row => row.QuranWordId).First();
                return new
                {
                    LemmaId = group.Key,
                    Item = new StemLemmaItemDto(group.Key, first.LemmaText, first.LemmaBuckwalter, group.Count()),
                    FirstQuranWordId = first.QuranWordId,
                };
            })
            .OrderByDescending(row => row.Item.OccurrencesCount)
            .ThenBy(row => row.FirstQuranWordId)
            .ThenBy(row => row.LemmaId)
            .Select(row => row.Item)
            .ToList();
    }
}
