using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Linking;

public static class LinkingPreflightProjection
{
    public static LinkingPreflightResultDto ToResult(
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification,
        string preflightToken) =>
        new(
            state.DoorId,
            state.DoorName,
            classification.IsNoOp,
            classification.IsBlocked,
            preflightToken,
            ToCounts(classification.Totals),
            [.. classification.Sources.Select(ToSource)]);

    private static LinkingSourcePreflightDto ToSource(LinkingSourceClassification source) =>
        new(
            source.Source.SourceIdentity,
            source.Source.Label,
            LinkingSourceTokens.ToToken(source.Source.SourceKind),
            LinkingOperationTokens.ToToken(source.Source.ContributionMode),
            source.Source.AutomaticWordMatchesEnabled,
            LinkingPreflightTokens.ToToken(source.Classification),
            source.ExistingContributionId,
            source.ExistingContributionVersion,
            ToCounts(source.Counts),
            [.. source.Ayahs.Select(ToAyah)]);

    private static LinkingAyahPreflightDto ToAyah(LinkingAyahClassification ayah) =>
        new(
            ayah.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            LinkingPreflightTokens.ToToken(ayah.Classification),
            [
                .. ayah.OverlappingSources.Select(source => new LinkingOverlappingSourceDto(
                    source.SourceIdentity, source.Label, source.SourceKind))
            ],
            new LinkingWordChangesDto(
                ayah.WordChanges.Added, ayah.WordChanges.Removed, ayah.WordChanges.Unchanged),
            new LinkingDoorWordImpactDto(
                ayah.DoorWordImpact.Added,
                ayah.DoorWordImpact.Existing,
                ayah.DoorWordImpact.Removed),
            new LinkingDescriptionChangesDto(
                ayah.DescriptionChanges.Added,
                ayah.DescriptionChanges.Removed,
                ayah.DescriptionChanges.Changed,
                ayah.DescriptionChanges.Unchanged),
            LinkingPreflightTokens.ToToken(ayah.InvalidReason));

    private static LinkingPreflightCountsDto ToCounts(LinkingClassificationCounts counts) =>
        new(
            counts.Requested,
            counts.New,
            counts.Overlapping,
            counts.Unchanged,
            counts.Updated,
            counts.Removed,
            counts.Invalid);
}
