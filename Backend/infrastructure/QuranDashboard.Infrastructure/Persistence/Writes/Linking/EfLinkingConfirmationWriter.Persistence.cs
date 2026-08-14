using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<IReadOnlyDictionary<string, long>> PersistContributionsAsync(
        long operationId,
        int doorId,
        int actorUserId,
        ConfirmationWorkset workset,
        LockedConfirmationState loaded,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var contributionIds = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var batch in BatchesOf(workset.Sources))
        {
            var created = new List<(WorksetSource Source, LinkingSourceContribution Entity)>();

            foreach (var source in batch)
            {
                if (source.Classification.Classification == LinkingPreflightClassification.NewSource)
                {
                    var contribution = NewContribution(
                        operationId,
                        doorId,
                        actorUserId,
                        source.Request,
                        source.Classification.Source,
                        now);
                    db.LinkingSourceContributions.Add(contribution);
                    created.Add((source, contribution));
                    continue;
                }

                var existingId = source.Classification.ExistingContributionId!.Value;
                var existing = loaded.ContributionsById[existingId];
                db.LinkingSourceContributions.Attach(existing);
                db.Entry(existing)
                    .Property(contribution => contribution.Version)
                    .OriginalValue = source.Request.ExistingContributionVersion!.Value;
                StampContribution(
                    existing,
                    operationId,
                    actorUserId,
                    source.Request,
                    source.Classification.Source,
                    now);
                contributionIds.Add(source.Classification.Source.SourceIdentity, existing.Id);
            }

            await SaveTranslatingWriteExceptionsAsync(cancellationToken);

            foreach (var (source, entity) in created)
            {
                contributionIds.Add(source.Classification.Source.SourceIdentity, entity.Id);
            }

            DetachRange(created.Select(entry => entry.Entity));
            DetachRange(batch
                .Where(source => source.Classification.Classification == LinkingPreflightClassification.Update)
                .Select(source => loaded.ContributionsById[source.Classification.ExistingContributionId!.Value]));
        }

        return contributionIds;
    }

    private static LinkingSourceContribution NewContribution(
        long operationId,
        int doorId,
        int actorUserId,
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent source,
        DateTimeOffset now)
    {
        var form = LinkingSourceStorage.Encode(request.Descriptor, source.SourceIdentity);

        return new LinkingSourceContribution
        {
            OperationId = operationId,
            DoorId = doorId,
            OrderValue = source.OrderValue,
            ContributionMode = source.ContributionMode,
            SourceKind = form.Kind,
            SourceIdentity = form.SourceIdentity,
            SourceIdentityHash = form.SourceIdentityHash,
            Label = form.Label,
            ScopeJson = form.ScopeJson,
            RootId = form.RootId,
            LemmaId = form.LemmaId,
            StemId = form.StemId,
            UniqueSimpleWordId = form.UniqueSimpleWordId,
            UniqueTashkeelWordId = form.UniqueTashkeelWordId,
            WordTypeTashkeelWordId = form.WordTypeTashkeelWordId,
            ResolvedAyahCount = source.ResolvedAyahCount,
            ResolvedAtUtc = source.ResolvedAtUtc,
            CreatedAtUtc = now,
            CreatedBy = actorUserId,
            UpdatedAtUtc = now,
            UpdatedBy = actorUserId,
        };
    }

    private static void StampContribution(
        LinkingSourceContribution contribution,
        long operationId,
        int actorUserId,
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent source,
        DateTimeOffset now)
    {
        var form = LinkingSourceStorage.Encode(request.Descriptor, source.SourceIdentity);

        contribution.OperationId = operationId;
        contribution.OrderValue = source.OrderValue;
        contribution.ContributionMode = source.ContributionMode;
        contribution.SourceKind = form.Kind;
        contribution.SourceIdentity = form.SourceIdentity;
        contribution.SourceIdentityHash = form.SourceIdentityHash;
        contribution.Label = form.Label;
        contribution.ScopeJson = form.ScopeJson;
        contribution.RootId = form.RootId;
        contribution.LemmaId = form.LemmaId;
        contribution.StemId = form.StemId;
        contribution.UniqueSimpleWordId = form.UniqueSimpleWordId;
        contribution.UniqueTashkeelWordId = form.UniqueTashkeelWordId;
        contribution.WordTypeTashkeelWordId = form.WordTypeTashkeelWordId;
        contribution.ResolvedAyahCount = source.ResolvedAyahCount;
        contribution.ResolvedAtUtc = source.ResolvedAtUtc;
        contribution.UpdatedAtUtc = now;
        contribution.UpdatedBy = actorUserId;
    }
}
