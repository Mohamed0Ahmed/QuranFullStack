using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingWordTypeSelectionBodyMapper
{
    internal static bool TryMap(
        LinkingWordTypeSelectionBody? selection,
        out LinkingWordTypeSelection mapped,
        out LinkingDescriptorViolation violation)
    {
        mapped = null!;
        violation = null!;

        if (selection is null)
        {
            violation = LinkingBodyViolations.Malformed("selection");
            return false;
        }

        if (!LinkingSourceTokens.TryParseWordTypeSelection(selection.Kind, out var selectionKind))
        {
            violation = LinkingBodyViolations.Malformed("selection.kind", selection.Kind);
            return false;
        }

        if (!TryMapScope(selection.Scope, out var scope, out violation))
        {
            return false;
        }

        if (selectionKind == LinkingWordTypeSelectionKind.Word)
        {
            return TryMapWord(selection, scope, out mapped, out violation);
        }

        var (dimensionId, dimensionField) = selectionKind switch
        {
            LinkingWordTypeSelectionKind.Root => (selection.RootId, "selection.rootId"),
            LinkingWordTypeSelectionKind.Stem => (selection.StemId, "selection.stemId"),
            _ => (selection.LemmaId, "selection.lemmaId"),
        };

        if (!LinkingBodyViolations.TryIdentifier(dimensionId, dimensionField, out violation))
        {
            return false;
        }

        mapped = new LinkingWordTypeSelection.Dimension(selectionKind, dimensionId!.Value, scope);
        return true;
    }

    private static bool TryMapWord(
        LinkingWordTypeSelectionBody selection,
        LinkingWordTypeScope scope,
        out LinkingWordTypeSelection mapped,
        out LinkingDescriptorViolation violation)
    {
        mapped = null!;

        if (!LinkingBodyViolations.TryIdentifier(
                selection.TashkeelWordId,
                "selection.tashkeelWordId",
                out violation))
        {
            return false;
        }

        if (LinkingSourceDescriptorValidation.RequiredTextError(
                selection.ContextCode,
                "selection.contextCode") is not null)
        {
            violation = LinkingBodyViolations.Malformed("selection.contextCode", selection.ContextCode);
            return false;
        }

        if (!LinkingBodyViolations.TryToken(
                selection.Case, LinkingWordTypeScope.CaseTokens, "selection.case", out violation)
            || !LinkingBodyViolations.TryToken(
                selection.Tense, LinkingWordTypeScope.TenseTokens, "selection.tense", out violation)
            || !LinkingBodyViolations.TryToken(
                selection.Voice, LinkingWordTypeScope.VoiceTokens, "selection.voice", out violation))
        {
            return false;
        }

        mapped = new LinkingWordTypeSelection.Word(
            selection.TashkeelWordId!.Value,
            selection.ContextCode!,
            selection.Case!,
            selection.Tense!,
            selection.Voice!,
            scope);
        return true;
    }

    private static bool TryMapScope(
        LinkingWordTypeScopeBody? scopeBody,
        out LinkingWordTypeScope scope,
        out LinkingDescriptorViolation violation)
    {
        scope = null!;
        violation = null!;

        if (scopeBody is null)
        {
            violation = LinkingBodyViolations.Malformed("selection.scope");
            return false;
        }

        if (!LinkingBodyViolations.TryToken(
                scopeBody.Type, LinkingWordTypeScope.TypeTokens, "selection.scope.type", out violation)
            || !LinkingBodyViolations.TryToken(
                scopeBody.Case, LinkingWordTypeScope.CaseTokens, "selection.scope.case", out violation)
            || !LinkingBodyViolations.TryToken(
                scopeBody.Tense, LinkingWordTypeScope.TenseTokens, "selection.scope.tense", out violation)
            || !LinkingBodyViolations.TryToken(
                scopeBody.Voice, LinkingWordTypeScope.VoiceTokens, "selection.scope.voice", out violation))
        {
            return false;
        }

        if (LinkingSourceDescriptorValidation.OptionalTextError(
                scopeBody.ChildCode,
                "selection.scope.childCode") is not null)
        {
            violation = LinkingBodyViolations.Malformed("selection.scope.childCode", scopeBody.ChildCode);
            return false;
        }

        scope = new LinkingWordTypeScope(
            scopeBody.Type!,
            scopeBody.ChildCode,
            scopeBody.Case!,
            scopeBody.Tense!,
            scopeBody.Voice!);
        return true;
    }
}
