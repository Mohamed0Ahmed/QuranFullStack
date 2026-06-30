using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;

public sealed class GetWordTypeSurahsHandler(
    ILogger<GetWordTypeSurahsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeSurahs";

    public async Task<GetWordTypeSurahsOutcome> HandleAsync(GetWordTypeSurahsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var identity = ToIdentity(query.TashkeelWordId, query.ContextCode, query.Case, query.Tense, query.Voice);

        if (!identity.IsValid)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {tashkeelWordId} {contextCode}", FeatureName, OperationName, "invalidIdentity", query.TashkeelWordId, query.ContextCode);
            return new GetWordTypeSurahsOutcome.InvalidIdentity();
        }

        var surahs = await reader.GetSurahsAsync(identity, cancellationToken);
        return surahs is null ? new GetWordTypeSurahsOutcome.NotFound() : new GetWordTypeSurahsOutcome.Success(surahs);
    }

    private static WordTypeRowIdentity ToIdentity(int id, string? contextCode, string? @case, string? tense, string? voice) =>
        new(id, string.IsNullOrWhiteSpace(contextCode) ? string.Empty : contextCode.Trim(), Normalize(@case), Normalize(tense), Normalize(voice));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
