
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;

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
