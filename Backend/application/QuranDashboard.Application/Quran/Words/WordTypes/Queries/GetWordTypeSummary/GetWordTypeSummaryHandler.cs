using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;

public sealed class GetWordTypeSummaryHandler(
    ILogger<GetWordTypeSummaryHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeSummary";

    public async Task<GetWordTypeSummaryOutcome> HandleAsync(GetWordTypeSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var identity = ToIdentity(query.TashkeelWordId, query.ContextCode, query.Case, query.Tense, query.Voice);
        if (!identity.IsValid || !WordTypesHandlerValidation.IsValidIdentitySecondaryValues(query.Case, query.Tense, query.Voice))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {tashkeelWordId} {contextCode}",
                FeatureName,
                OperationName,
                "invalidIdentity",
                query.TashkeelWordId,
                query.ContextCode);
            return new GetWordTypeSummaryOutcome.InvalidIdentity();
        }

        var summary = await reader.GetSummaryAsync(identity, cancellationToken);
        return summary is null
            ? new GetWordTypeSummaryOutcome.NotFound()
            : new GetWordTypeSummaryOutcome.Success(summary);
    }

    private static WordTypeRowIdentity ToIdentity(int id, string? contextCode, string? @case, string? tense, string? voice) =>
        new(id, string.IsNullOrWhiteSpace(contextCode) ? string.Empty : contextCode.Trim(), Normalize(@case), Normalize(tense), Normalize(voice));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
