using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<IReadOnlySet<long>> SynchronizeContributionLinksAsync(
        ConfirmationWorkset workset,
        IReadOnlyDictionary<string, long> contributionIds,
        IReadOnlyDictionary<string, long> unitIds,
        LockedConfirmationState loaded,
        CancellationToken cancellationToken)
    {
        var existingByContribution = loaded.ContributionUnits
            .GroupBy(link => link.SourceContributionId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(link => link.UnitId));
        var stale = new List<LinkingSourceContributionUnit>();
        var reordered = new List<ContributionLinkOrderChange>();
        var missing = new List<LinkingSourceContributionUnit>();

        foreach (var source in workset.Sources)
        {
            var contributionId = contributionIds[source.Classification.Source.SourceIdentity];
            var existing = existingByContribution.GetValueOrDefault(
                contributionId,
                new Dictionary<long, LinkingSourceContributionUnit>());
            var desired = source.Units.ToDictionary(
                unit => unitIds[unit.Unit.Intent.Identity],
                unit => unit.OrderValue);

            stale.AddRange(existing.Values.Where(link => !desired.ContainsKey(link.UnitId)));

            foreach (var (unitId, orderValue) in desired)
            {
                if (!existing.TryGetValue(unitId, out var link))
                {
                    missing.Add(new LinkingSourceContributionUnit
                    {
                        SourceContributionId = contributionId,
                        UnitId = unitId,
                        OrderValue = orderValue,
                    });
                }
                else if (link.OrderValue != orderValue)
                {
                    reordered.Add(new ContributionLinkOrderChange(link, orderValue));
                }
            }
        }

        await RemoveStaleContributionLinksAsync(stale, cancellationToken);
        await MoveContributionLinksToTemporaryOrdersAsync(reordered, cancellationToken);
        await MoveContributionLinksToFinalOrdersAsync(reordered, cancellationToken);
        await InsertContributionLinksAsync(missing, cancellationToken);

        return stale.Select(link => link.UnitId).ToHashSet();
    }

    private async Task RemoveStaleContributionLinksAsync(
        IReadOnlyList<LinkingSourceContributionUnit> links,
        CancellationToken cancellationToken)
    {
        foreach (var batch in BatchesOf(links))
        {
            db.LinkingSourceContributionUnits.AttachRange(batch);
            db.LinkingSourceContributionUnits.RemoveRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        }
    }

    private async Task MoveContributionLinksToTemporaryOrdersAsync(
        IReadOnlyList<ContributionLinkOrderChange> changes,
        CancellationToken cancellationToken)
    {
        foreach (var batch in BatchesOf(changes))
        {
            var links = batch.Select(change => change.Link).ToList();
            db.LinkingSourceContributionUnits.AttachRange(links);

            foreach (var change in batch)
            {
                change.Link.OrderValue = int.MinValue + change.FinalOrderValue;
            }

            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(links);
        }
    }

    private async Task MoveContributionLinksToFinalOrdersAsync(
        IReadOnlyList<ContributionLinkOrderChange> changes,
        CancellationToken cancellationToken)
    {
        foreach (var batch in BatchesOf(changes))
        {
            var links = batch.Select(change => change.Link).ToList();
            db.LinkingSourceContributionUnits.AttachRange(links);

            foreach (var change in batch)
            {
                change.Link.OrderValue = change.FinalOrderValue;
            }

            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(links);
        }
    }

    private async Task InsertContributionLinksAsync(
        IReadOnlyList<LinkingSourceContributionUnit> links,
        CancellationToken cancellationToken)
    {
        foreach (var batch in BatchesOf(links))
        {
            db.LinkingSourceContributionUnits.AddRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(batch);
        }
    }

    private sealed record ContributionLinkOrderChange(
        LinkingSourceContributionUnit Link,
        int FinalOrderValue);
}
