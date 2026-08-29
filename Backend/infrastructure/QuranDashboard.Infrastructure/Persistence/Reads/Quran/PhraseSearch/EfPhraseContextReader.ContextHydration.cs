using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private async Task<IReadOnlyDictionary<long, ContextOccurrence>> LoadContextOccurrencesAsync(
        IReadOnlyList<ContextOccurrenceRow> rows,
        PhraseResolutionReference resolution,
        CancellationToken cancellationToken)
    {
        var uniqueRows = rows
            .DistinctBy(row => row.OccurrenceId)
            .ToList();
        if (uniqueRows.Count == 0)
        {
            return new Dictionary<long, ContextOccurrence>();
        }

        var ayahIds = uniqueRows
            .Select(row => row.AyahId)
            .Distinct()
            .ToList();
        var wordRows = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new ContextWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani,
                resolution.Mode == PhraseTextMode.Simple
                    ? word.UniqueSimpleWordId
                    : word.UniqueTashkeelWordId))
            .ToListAsync(cancellationToken);
        if (wordRows.Any(word => word.ExactTokenId is null))
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        var wordsByAyah = wordRows
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ContextWord>)group
                    .Select(word => new ContextWord(
                        word.QuranWordId,
                        word.WordNumber,
                        word.PageNumber,
                        word.TextUthmani,
                        word.ExactTokenId!.Value))
                    .ToList());
        var occurrences = new Dictionary<long, ContextOccurrence>(uniqueRows.Count);
        foreach (var row in uniqueRows)
        {
            var words = wordsByAyah.GetValueOrDefault(row.AyahId)
                ?? throw new InvalidDataException("PhraseSearch context occurrence has no source words.");
            if (words.Count == 0
                || words.Where((word, index) => word.WordNumber != index + 1).Any()
                || row.StartWordNumber <= 0
                || row.EndWordNumber > words.Count)
            {
                throw new InvalidDataException("PhraseSearch context occurrence does not map to a contiguous Quran ayah.");
            }

            var occurrence = new ContextOccurrence(row, words);
            var exactTokenIds = occurrence.Words
                .Skip(row.StartWordNumber - 1)
                .Take(row.EndWordNumber - row.StartWordNumber + 1)
                .Select(word => word.ExactTokenId);
            if (!exactTokenIds.SequenceEqual(resolution.ExactTokenIds))
            {
                throw new InvalidDataException("PhraseSearch context occurrence does not match its resolved exact identity.");
            }

            occurrences.Add(row.OccurrenceId, occurrence);
        }

        return occurrences;
    }
}
