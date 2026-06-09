
namespace QuranDashboard.Application.Quran.Import.Validation;

internal static class ImportValidationWordIndex
{
    public static Dictionary<int, QuranWord> ById(IReadOnlyList<QuranWord> words)
    {
        var wordsById = new Dictionary<int, QuranWord>();

        foreach (var word in words)
        {
            wordsById.TryAdd(word.Id, word);
        }

        return wordsById;
    }
}
