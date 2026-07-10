
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;

internal sealed class SourceAlignmentCheck
{
    public ImportCheckResult Evaluate(QuranImportSourceData source)
    {
        var mismatches = CountSourceMismatches(source);

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.SourceAlignment,
            "0",
            mismatches.ToString(),
            mismatches == 0);
    }

    private static int CountSourceMismatches(QuranImportSourceData source)
    {
        var mismatches = 0;
        mismatches += CountAlignmentMismatches(source.Glyph, source.Uthmani);
        mismatches += CountAlignmentMismatches(source.Glyph, source.UthmaniSimple);
        mismatches += CountAlignmentMismatches(source.Glyph, source.ImlaeiSimple);
        return mismatches;
    }

    private static int CountAlignmentMismatches(
        IReadOnlyList<WordRecordDto> baseline,
        IReadOnlyList<WordRecordDto> candidate)
    {
        var candidateByLocation = candidate.ToDictionary(record => record.Location, StringComparer.Ordinal);
        var mismatches = 0;

        foreach (var record in baseline)
        {
            if (!candidateByLocation.TryGetValue(record.Location, out var other) ||
                other.Id != record.Id ||
                other.Surah != record.Surah ||
                other.Ayah != record.Ayah ||
                other.Word != record.Word)
            {
                mismatches++;
            }
        }

        return mismatches;
    }
}
