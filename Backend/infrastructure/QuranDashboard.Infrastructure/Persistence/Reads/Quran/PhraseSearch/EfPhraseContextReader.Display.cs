using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private async Task<IReadOnlyDictionary<int, string>> LoadExactTokenTextsAsync(
        PhraseTextMode mode,
        IEnumerable<int> exactTokenIds,
        CancellationToken cancellationToken)
    {
        var ids = exactTokenIds
            .Distinct()
            .Order()
            .ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var rows = mode == PhraseTextMode.Simple
            ? await db.QuranWordsUniqueSimple
                .AsNoTracking()
                .Where(word => ids.Contains(word.Id))
                .Select(word => new ExactTokenText(word.Id, word.TextUthmani))
                .ToListAsync(cancellationToken)
            : await db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .Where(word => ids.Contains(word.Id))
                .Select(word => new ExactTokenText(word.Id, word.TextUthmani))
                .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.Id, row => row.TextUthmani);
    }

    private sealed record ExactTokenText(int Id, string TextUthmani);
}
