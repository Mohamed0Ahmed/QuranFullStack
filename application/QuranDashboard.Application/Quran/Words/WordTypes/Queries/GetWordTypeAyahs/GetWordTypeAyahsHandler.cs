using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;

public sealed class GetWordTypeAyahsHandler(
    ILogger<GetWordTypeAyahsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeAyahs";

    public async Task<GetWordTypeAyahsOutcome> HandleAsync(GetWordTypeAyahsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var identity = ToIdentity(query.TashkeelWordId, query.ContextCode, query.Case, query.Tense, query.Voice);

        if (!identity.IsValid)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {tashkeelWordId} {contextCode}", FeatureName, OperationName, "invalidIdentity", query.TashkeelWordId, query.ContextCode);
            return new GetWordTypeAyahsOutcome.InvalidIdentity();
        }

        if (!WordTypesHandlerValidation.IsValidPaging(query.Page, query.PageSize))
        {
            return new GetWordTypeAyahsOutcome.InvalidPaging();
        }

        var page = await reader.GetAyahMatchesAsync(identity, query.Page, query.PageSize, cancellationToken);
        return page is null ? new GetWordTypeAyahsOutcome.NotFound() : new GetWordTypeAyahsOutcome.Success(page);
    }

    private static WordTypeRowIdentity ToIdentity(int id, string? contextCode, string? @case, string? tense, string? voice) =>
        new(id, string.IsNullOrWhiteSpace(contextCode) ? string.Empty : contextCode.Trim(), Normalize(@case), Normalize(tense), Normalize(voice));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
