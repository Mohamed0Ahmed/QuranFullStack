using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private static PhraseContextAyahDto CreateContextAyah(
        IReadOnlyList<ContextOccurrence> occurrences,
        PhraseContextSelection selection)
    {
        if (occurrences.Count == 0)
        {
            throw new InvalidDataException("PhraseSearch result ayah has no matching occurrence.");
        }

        var first = occurrences[0];
        if (occurrences.Any(occurrence => occurrence.Row.AyahId != first.Row.AyahId))
        {
            throw new InvalidDataException("PhraseSearch result aggregation mixed ayahs.");
        }

        var queryWordIds = new HashSet<int>();
        var previousWordIds = new HashSet<int>();
        var followingWordIds = new HashSet<int>();
        foreach (var occurrence in occurrences)
        {
            foreach (var word in QueryWords(occurrence))
            {
                queryWordIds.Add(word.QuranWordId);
            }

            foreach (var word in SelectedContextWords(occurrence, selection, PhraseContextSide.Previous))
            {
                previousWordIds.Add(word.QuranWordId);
            }

            foreach (var word in SelectedContextWords(occurrence, selection, PhraseContextSide.Following))
            {
                followingWordIds.Add(word.QuranWordId);
            }
        }

        return new PhraseContextAyahDto(
            first.Row.AyahId,
            first.Row.VerseKey,
            first.Row.SurahNumber,
            first.Row.SurahNameArabic,
            first.Row.AyahNumber,
            first.Row.PageFrom,
            first.Row.PageTo,
            first.Words
                .Select(word => new PhraseAyahWordDto(
                    word.QuranWordId,
                    word.WordNumber,
                    word.PageNumber,
                    word.TextUthmani))
                .ToList(),
            occurrences.Count,
            new PhraseContextHighlightsDto(
                OrderedWordIds(first.Words, queryWordIds),
                OrderedWordIds(first.Words, previousWordIds),
                OrderedWordIds(first.Words, followingWordIds)));
    }

    private static IEnumerable<ContextWord> QueryWords(ContextOccurrence occurrence) => occurrence.Words
        .Skip(occurrence.Row.StartWordNumber - 1)
        .Take(occurrence.Row.EndWordNumber - occurrence.Row.StartWordNumber + 1);

    private static IEnumerable<ContextWord> SelectedContextWords(
        ContextOccurrence occurrence,
        PhraseContextSelection selection,
        PhraseContextSide side)
    {
        var path = side == PhraseContextSide.Previous ? selection.Previous : selection.Following;
        var alternatives = side == PhraseContextSide.Previous
            ? selection.PreviousAlternatives
            : selection.FollowingAlternatives;
        var pathLength = path?.SelectedExactTokenIds.Count ?? 0;
        for (var index = 0; index < pathLength; index++)
        {
            yield return ContextWordAt(occurrence, side, index);
        }

        if (alternatives is not null)
        {
            var alternativeWord = ContextWordAt(occurrence, side, pathLength);
            if (!alternatives.AlternativeExactTokenIds.Contains(alternativeWord.ExactTokenId))
            {
                throw new InvalidDataException("PhraseSearch result occurrence does not match its alternative context.");
            }

            yield return alternativeWord;
        }
    }

    private static ContextWord ContextWordAt(
        ContextOccurrence occurrence,
        PhraseContextSide side,
        int offset)
    {
        var index = side == PhraseContextSide.Previous
            ? occurrence.Row.StartWordNumber - 2 - offset
            : occurrence.Row.EndWordNumber + offset;
        if (index < 0 || index >= occurrence.Words.Count)
        {
            throw new InvalidDataException("PhraseSearch result context exceeds the ayah boundary.");
        }

        return occurrence.Words[index];
    }

    private static IReadOnlyList<int> OrderedWordIds(
        IReadOnlyList<ContextWord> words,
        IReadOnlySet<int> wordIds) => words
        .Where(word => wordIds.Contains(word.QuranWordId))
        .Select(word => word.QuranWordId)
        .ToList();
}
