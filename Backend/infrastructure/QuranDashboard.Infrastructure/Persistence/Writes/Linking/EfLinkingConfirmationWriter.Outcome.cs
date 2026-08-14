using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

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

    private static LinkingOperation CreatePreparedOperation(
        LinkingConfirmationJobLease lease,
        LinkingPreparedPreflight preflight,
        DateTimeOffset now) =>
        new()
        {
            DoorId = lease.DoorId,
            ActorUserId = lease.ActorUserId,
            IdempotencyKey = lease.IdempotencyKey,
            PreparedPreflightId = preflight.Id,
            PreparedPreflightReferenceId = preflight.Id,
            ConfirmationJobReferenceId = lease.JobId,
            RequestContractKind = LinkingConfirmationRequestContracts.PreparedJob,
            RequestSchemaVersion = LinkingConfirmationRequestContracts.SchemaVersion,
            RequestHash = lease.RequestHash,
            LinkingDataRevision = preflight.LinkingDataRevision,
            ConfirmedAtUtc = now,
            SourceCount = preflight.TotalSources,
            AyahCount = preflight.RequestedCount!.Value,
            OutcomeJson = EmptyOutcomeJson,
        };

    private async Task<LinkingConfirmationResultDto> CreatePreparedResultAsync(
        LinkingPreparedPreflight preflight,
        CancellationToken cancellationToken)
    {
        var sources = await db.LinkingPreparedSources.AsNoTracking()
            .Where(source => source.PreflightId == preflight.Id)
            .OrderBy(source => source.OrderValue)
            .ToListAsync(cancellationToken);
        var identities = sources.Select(source => source.ContributionIdentity).ToList();
        var contributionIds = identities.Count == 0
            ? new Dictionary<string, long>(StringComparer.Ordinal)
            : await db.LinkingSourceContributions.AsNoTracking()
                .Where(contribution => contribution.DoorId == preflight.DoorId
                    && contribution.DeletedAtUtc == null
                    && identities.Contains(contribution.SourceIdentity))
                .ToDictionaryAsync(
                    contribution => contribution.SourceIdentity,
                    contribution => contribution.Id,
                    StringComparer.Ordinal,
                    cancellationToken);
        var sourceResults = sources.Select(source => new LinkingConfirmationSourceResultDto(
            source.ContributionIdentity,
            source.Classification
                ?? throw new InvalidOperationException("A ready prepared source has no classification."),
            contributionIds.TryGetValue(source.ContributionIdentity, out var contributionId)
                ? contributionId
                : source.ExistingContributionId,
            CountsOf(source)))
            .ToList();
        return new LinkingConfirmationResultDto(
            preflight.DoorId,
            preflight.IsNoOp!.Value,
            CountsOf(preflight),
            sourceResults);
    }

    private static LinkingPreflightCountsDto CountsOf(LinkingPreparedPreflight preflight) =>
        new(
            preflight.RequestedCount!.Value,
            preflight.NewCount!.Value,
            preflight.OverlappingCount!.Value,
            preflight.UnchangedCount!.Value,
            preflight.UpdatedCount!.Value,
            preflight.RemovedCount!.Value,
            preflight.InvalidCount!.Value);

    private static LinkingPreflightCountsDto CountsOf(LinkingPreparedSource source) =>
        new(
            source.RequestedCount!.Value,
            source.NewCount!.Value,
            source.OverlappingCount!.Value,
            source.UnchangedCount!.Value,
            source.UpdatedCount!.Value,
            source.RemovedCount!.Value,
            source.InvalidCount!.Value);

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
