using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<LinkingSourceContribution> InsertContributionAsync(
        long operationId,
        int doorId,
        int actorUserId,
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent source,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var contribution = NewContribution(operationId, doorId, actorUserId, request, source, now);
        db.LinkingSourceContributions.Add(contribution);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await InsertChildrenAsync(contribution.Id, actorUserId, source, now, cancellationToken);

        return contribution;
    }

    private async Task ReplaceContributionAsync(
        long operationId,
        int actorUserId,
        LinkingSourceContribution contribution,
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent source,
        LockedConfirmationState loaded,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var units = loaded.Units
            .Where(unit => unit.SourceContributionId == contribution.Id)
            .ToList();
        var unitIds = units.Select(unit => unit.Id).ToHashSet();
        var unitAyahs = loaded.UnitAyahs
            .Where(unitAyah => unitIds.Contains(unitAyah.UnitId))
            .ToList();
        var unitAyahIds = unitAyahs.Select(unitAyah => unitAyah.Id).ToHashSet();

        db.LinkingUnitAyahDescriptions.RemoveRange(
            loaded.Descriptions.Where(description => unitAyahIds.Contains(description.UnitAyahId)));
        db.LinkingUnitAyahWords.RemoveRange(
            loaded.Words.Where(word => unitAyahIds.Contains(word.UnitAyahId)));
        db.LinkingUnitAyahs.RemoveRange(unitAyahs);
        db.LinkingUnits.RemoveRange(units);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        StampContribution(contribution, operationId, actorUserId, request, source, now);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await InsertChildrenAsync(contribution.Id, actorUserId, source, now, cancellationToken);
    }

    private static LinkingSourceContribution NewContribution(
        long operationId,
        int doorId,
        int actorUserId,
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent source,
        DateTimeOffset now)
    {
        var form = LinkingSourceStorage.Encode(request.Descriptor);

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
        var form = LinkingSourceStorage.Encode(request.Descriptor);

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

    private async Task InsertChildrenAsync(
        long contributionId,
        int actorUserId,
        LinkingOperationSourceIntent source,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var units = source.Units
            .Select((_, index) => new LinkingUnit
            {
                SourceContributionId = contributionId,
                OrderValue = index + 1,
                IsGrouped = source.ContributionMode == LinkingContributionMode.ManualGrouped,
            })
            .ToList();

        db.LinkingUnits.AddRange(units);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        var createdAyahs = new List<(LinkingUnitAyah Entity, LinkingOperationAyahIntent Intent)>();

        for (var unitIndex = 0; unitIndex < source.Units.Count; unitIndex++)
        {
            for (var ayahIndex = 0; ayahIndex < source.Units[unitIndex].Ayahs.Count; ayahIndex++)
            {
                var intent = source.Units[unitIndex].Ayahs[ayahIndex];
                var entity = new LinkingUnitAyah
                {
                    UnitId = units[unitIndex].Id,
                    SourceContributionId = contributionId,
                    AyahId = intent.AyahId,
                    OrderValue = ayahIndex + 1,
                };

                db.LinkingUnitAyahs.Add(entity);
                createdAyahs.Add((entity, intent));
            }
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        foreach (var (entity, intent) in createdAyahs)
        {
            db.LinkingUnitAyahWords.AddRange(intent.WordIds.Select(wordId => new LinkingUnitAyahWord
            {
                UnitAyahId = entity.Id,
                QuranWordId = wordId,
                AyahId = intent.AyahId,
            }));

            db.LinkingUnitAyahDescriptions.AddRange(intent.Descriptions.Select((body, index) =>
                new LinkingUnitAyahDescription
                {
                    UnitAyahId = entity.Id,
                    OrderValue = index + 1,
                    Body = body,
                    CreatedAtUtc = now,
                    CreatedBy = actorUserId,
                    UpdatedAtUtc = now,
                    UpdatedBy = actorUserId,
                }));
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }
}
