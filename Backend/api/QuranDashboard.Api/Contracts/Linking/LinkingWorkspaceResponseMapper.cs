using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingWorkspaceResponseMapper
{
    internal static LinkingWorkspaceResponse ToResponse(LinkingWorkspaceDto workspace) =>
        new()
        {
            WorkspaceVersion = workspace.WorkspaceVersion,
            Sources = [.. workspace.Sources.Select(ToResponse)],
        };

    private static LinkingWorkspaceSourceResponse ToResponse(LinkingWorkspaceSourceDto source) =>
        new()
        {
            Id = source.Id,
            SourceVersion = source.SourceVersion,
            OrderValue = source.OrderValue,
            Descriptor = ToBody(source.Descriptor),
            SourceIdentity = source.SourceIdentity,
            InclusionMode = source.InclusionMode,
            AyahOverrides = source.AyahOverrides,
            SelectedWords =
            [
                .. source.SelectedWords.Select(word => new LinkingSelectedWordBody
                {
                    AyahId = word.AyahId,
                    QuranWordId = word.QuranWordId,
                })
            ],
            AutomaticWordMatchesEnabled = source.AutomaticWordMatchesEnabled,
            ManualLinkShape = source.ManualLinkShape,
            ManualAyahs =
            [
                .. source.ManualAyahs.Select(manualAyah => new LinkingWorkspaceManualAyahResponse
                {
                    AyahId = manualAyah.AyahId,
                    VerseKey = manualAyah.VerseKey,
                    OrderValue = manualAyah.OrderValue,
                    PageHint = manualAyah.PageHint,
                })
            ],
            Descriptions =
            [
                .. source.Descriptions.Select(description => new LinkingWorkspaceDescriptionResponse
                {
                    AyahId = description.AyahId,
                    OrderValue = description.OrderValue,
                    Body = description.Body,
                })
            ],
            LastResolvedCount = source.LastResolvedCount,
            LastResolvedAtUtc = source.LastResolvedAtUtc,
        };

    private static LinkingSourceDescriptorBody ToBody(LinkingSourceDescriptor descriptor)
    {
        var body = new LinkingSourceDescriptorBody
        {
            Kind = LinkingSourceTokens.ToToken(descriptor.Kind),
            Label = descriptor.Label,
        };

        return descriptor switch
        {
            LinkingSourceDescriptor.Root source => body with
            {
                RootId = source.RootId,
                TypeCodes = source.TypeCodes,
            },
            LinkingSourceDescriptor.Lemma source => body with
            {
                LemmaId = source.LemmaId,
                TypeCode = source.TypeCodes.Count == 1 ? source.TypeCodes[0] : null,
                TypeCodes = source.TypeCodes,
            },
            LinkingSourceDescriptor.Stem source => body with
            {
                StemId = source.StemId,
                TypeCode = source.TypeCodes.Count == 1 ? source.TypeCodes[0] : null,
                TypeCodes = source.TypeCodes,
            },
            LinkingSourceDescriptor.UniqueWord source => body with
            {
                Mode = LinkingSourceTokens.ToToken(source.Mode),
                WordId = source.WordId,
                TypeCodes = source.TypeCodes,
            },
            LinkingSourceDescriptor.WordType source => body with
            {
                Selection = ToBody(source.Selection),
            },
            LinkingSourceDescriptor.ManualMushafAyahs source => body with
            {
                ContextKey = source.ContextKey,
                ManualAyahs =
                [
                    .. source.VerseKeys.Select(verseKey => new LinkingManualAyahBody
                    {
                        VerseKey = verseKey.Value,
                    })
                ],
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor), descriptor.Kind, "Unknown linking source kind."),
        };
    }

    private static LinkingWordTypeSelectionBody ToBody(LinkingWordTypeSelection selection)
    {
        var body = new LinkingWordTypeSelectionBody
        {
            Kind = LinkingSourceTokens.ToToken(selection.Kind),
            Scope = new LinkingWordTypeScopeBody
            {
                Type = selection.Scope.Type,
                ChildCode = selection.Scope.ChildCode,
                Case = selection.Scope.Case,
                Tense = selection.Scope.Tense,
                Voice = selection.Scope.Voice,
            },
        };

        return selection switch
        {
            LinkingWordTypeSelection.Word word => body with
            {
                TashkeelWordId = word.TashkeelWordId,
                ContextCode = word.ContextCode,
                Case = word.Case,
                Tense = word.Tense,
                Voice = word.Voice,
            },
            LinkingWordTypeSelection.Dimension { Kind: LinkingWordTypeSelectionKind.Root } dimension =>
                body with { RootId = dimension.DimensionId },
            LinkingWordTypeSelection.Dimension { Kind: LinkingWordTypeSelectionKind.Stem } dimension =>
                body with { StemId = dimension.DimensionId },
            LinkingWordTypeSelection.Dimension dimension =>
                body with { LemmaId = dimension.DimensionId },
            _ => throw new ArgumentOutOfRangeException(
                nameof(selection), selection.Kind, "Unknown word type selection kind."),
        };
    }
}
