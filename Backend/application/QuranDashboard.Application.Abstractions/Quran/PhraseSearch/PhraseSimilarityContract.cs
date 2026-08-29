namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public static class PhraseSimilarityContract
{
    public const short MinimumLength = 2;
    public const short MinimumGlobalLength = 4;
    public const short DefaultGlobalLength = 4;
    public const short DefaultThreshold = 50;

    public static IReadOnlyList<short> Thresholds { get; } = [50, 60, 70, 80, 90];

    public static bool IsPresetThreshold(int value) =>
        value is >= short.MinValue and <= short.MaxValue
        && Thresholds.Contains((short)value);

    public static short MinimumMatchedWords(int wordCount, int threshold) => checked(
        (short)((wordCount * threshold + 99) / 100));
}

public sealed record PhraseHammingScore(
    short MatchedCount,
    short DifferenceCount,
    decimal MatchPercent,
    IReadOnlyList<short> MatchedPositions,
    IReadOnlyList<short> DifferingPositions)
{
    public static PhraseHammingScore Calculate(
        IReadOnlyList<int> leftExactTokenIds,
        IReadOnlyList<int> rightExactTokenIds)
    {
        ArgumentNullException.ThrowIfNull(leftExactTokenIds);
        ArgumentNullException.ThrowIfNull(rightExactTokenIds);
        if (leftExactTokenIds.Count == 0 || leftExactTokenIds.Count != rightExactTokenIds.Count)
        {
            throw new ArgumentException("PhraseSearch Hamming inputs must have the same non-zero length.");
        }

        var matchedPositions = new List<short>(leftExactTokenIds.Count);
        var differingPositions = new List<short>(leftExactTokenIds.Count);
        for (var index = 0; index < leftExactTokenIds.Count; index++)
        {
            var position = checked((short)(index + 1));
            if (leftExactTokenIds[index] == rightExactTokenIds[index])
            {
                matchedPositions.Add(position);
            }
            else
            {
                differingPositions.Add(position);
            }
        }

        var matchedCount = checked((short)matchedPositions.Count);
        var differenceCount = checked((short)differingPositions.Count);
        var matchPercent = decimal.Round(
            matchedCount * 100m / leftExactTokenIds.Count,
            1,
            MidpointRounding.AwayFromZero);
        return new PhraseHammingScore(
            matchedCount,
            differenceCount,
            matchPercent,
            matchedPositions,
            differingPositions);
    }
}
