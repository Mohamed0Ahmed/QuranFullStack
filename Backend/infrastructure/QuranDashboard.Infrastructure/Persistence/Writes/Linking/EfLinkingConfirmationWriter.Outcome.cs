using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private const int OutcomeSchemaVersion = 1;
    private const string EmptyOutcomeJson = "{\"schemaVersion\":1}";

    private static readonly JsonSerializerOptions OutcomeSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static LinkingConfirmationResultDto CreateResult(
        int doorId,
        bool isNoOp,
        LinkingOperationClassification classification,
        IReadOnlyDictionary<string, long?> contributionIds) =>
        new(
            doorId,
            isNoOp,
            ToCounts(classification.Totals),
            [
                .. classification.Sources.Select(source => new LinkingConfirmationSourceResultDto(
                    source.Source.SourceIdentity,
                    LinkingPreflightTokens.ToToken(source.Classification),
                    contributionIds[source.Source.SourceIdentity],
                    ToCounts(source.Counts)))
            ]);

    private static LinkingPreflightCountsDto ToCounts(LinkingClassificationCounts counts) =>
        new(
            counts.Requested,
            counts.New,
            counts.Overlapping,
            counts.Unchanged,
            counts.Updated,
            counts.Removed,
            counts.Invalid);

    private static string SerializeOutcome(LinkingConfirmationResultDto result)
    {
        var outcome = JsonSerializer.SerializeToNode(result, OutcomeSerializerOptions)?.AsObject()
            ?? throw new InvalidOperationException("Linking confirmation outcome serialization returned null.");
        outcome["schemaVersion"] = OutcomeSchemaVersion;

        return outcome.ToJsonString(OutcomeSerializerOptions);
    }

    private static LinkingConfirmationResultDto DeserializeOutcome(string outcomeJson) =>
        JsonSerializer.Deserialize<LinkingConfirmationResultDto>(outcomeJson, OutcomeSerializerOptions)
        ?? throw new InvalidOperationException("Stored linking confirmation outcome is empty.");
}
