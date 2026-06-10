
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Application.Quran.Import.Validation;

// Ensures the enriched imlaei-simple source carries a clean identity key (text_clean)
// for every record, so word_key_imlaei_simple can be bound without silent gaps.
internal sealed class ImlaeiCleanKeyCheck
{
    public ImportCheckResult Evaluate(QuranImportSourceData source)
    {
        var missing = source.ImlaeiSimple.Count(record => string.IsNullOrWhiteSpace(record.TextClean));

        return ImportCheckResults.Hard(
            ImportValidationCheckIds.ImlaeiCleanKey,
            "0",
            missing.ToString(),
            missing == 0);
    }
}
