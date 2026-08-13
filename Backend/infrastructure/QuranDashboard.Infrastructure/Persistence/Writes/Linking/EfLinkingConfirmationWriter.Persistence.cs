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
        await SynchronizeContributionUnitsAsync(
            contribution.Id,
            doorId,
            actorUserId,
            source,
            now,
            cancellationToken);

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
        db.LinkingSourceContributionUnits.RemoveRange(
            loaded.ContributionUnits.Where(link => link.SourceContributionId == contribution.Id));

        StampContribution(contribution, operationId, actorUserId, request, source, now);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await SynchronizeContributionUnitsAsync(
            contribution.Id,
            contribution.DoorId,
            actorUserId,
            source,
            now,
            cancellationToken);
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

    private async Task SynchronizeContributionUnitsAsync(
        long contributionId,
        int doorId,
        int actorUserId,
        LinkingOperationSourceIntent source,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < source.Units.Count; index++)
        {
            var unitIntent = source.Units[index];
            var identityHash = LinkingUnitIdentity.HashOf(unitIntent.Identity);
            var unit = await db.LinkingUnits
                .FirstOrDefaultAsync(candidate =>
                    candidate.DoorId == doorId && candidate.IdentityHash == identityHash,
                    cancellationToken);

            if (unit is not null
                && !string.Equals(unit.Identity, unitIntent.Identity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A linking unit identity hash collision was detected.");
            }

            if (unit is null)
            {
                unit = await InsertUnitAsync(
                    doorId,
                    actorUserId,
                    unitIntent,
                    now,
                    cancellationToken);
            }

            db.LinkingSourceContributionUnits.Add(new LinkingSourceContributionUnit
            {
                SourceContributionId = contributionId,
                UnitId = unit.Id,
                OrderValue = index + 1,
            });
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }

    private async Task<LinkingUnit> InsertUnitAsync(
        int doorId,
        int actorUserId,
        LinkingOperationUnitIntent intent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unit = new LinkingUnit
        {
            DoorId = doorId,
            Identity = intent.Identity,
            IdentityHash = LinkingUnitIdentity.HashOf(intent.Identity),
            IsGrouped = intent.IsGrouped,
            CreatedAtUtc = now,
            CreatedBy = actorUserId,
        };

        db.LinkingUnits.Add(unit);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        var createdAyahs = new List<(LinkingUnitAyah Entity, LinkingOperationAyahIntent Intent)>();

        for (var index = 0; index < intent.Ayahs.Count; index++)
        {
            var ayahIntent = intent.Ayahs[index];
            var entity = new LinkingUnitAyah
            {
                UnitId = unit.Id,
                AyahId = ayahIntent.AyahId,
                OrderValue = index + 1,
            };

            db.LinkingUnitAyahs.Add(entity);
            createdAyahs.Add((entity, ayahIntent));
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        foreach (var (entity, ayahIntent) in createdAyahs)
        {
            db.LinkingUnitAyahWords.AddRange(ayahIntent.WordIds.Select(wordId => new LinkingUnitAyahWord
            {
                UnitAyahId = entity.Id,
                QuranWordId = wordId,
                AyahId = ayahIntent.AyahId,
            }));

            db.LinkingUnitAyahDescriptions.AddRange(ayahIntent.Descriptions.Select((body, index) =>
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
        return unit;
    }

    private async Task RemoveOrphanUnitsAsync(int doorId, CancellationToken cancellationToken)
    {
        var orphanUnits = await db.LinkingUnits
            .Where(unit => unit.DoorId == doorId)
            .Where(unit => !db.LinkingSourceContributionUnits.Any(link => link.UnitId == unit.Id))
            .ToListAsync(cancellationToken);

        if (orphanUnits.Count == 0)
        {
            return;
        }

        var unitIds = orphanUnits.Select(unit => unit.Id).ToList();
        var unitAyahs = await db.LinkingUnitAyahs
            .Where(ayah => unitIds.Contains(ayah.UnitId))
            .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(ayah => ayah.Id).ToList();
        var words = await db.LinkingUnitAyahWords
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingUnitAyahDescriptions
            .Where(description => unitAyahIds.Contains(description.UnitAyahId))
            .ToListAsync(cancellationToken);

        db.LinkingUnitAyahDescriptions.RemoveRange(descriptions);
        db.LinkingUnitAyahWords.RemoveRange(words);
        db.LinkingUnitAyahs.RemoveRange(unitAyahs);
        db.LinkingUnits.RemoveRange(orphanUnits);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }
}
