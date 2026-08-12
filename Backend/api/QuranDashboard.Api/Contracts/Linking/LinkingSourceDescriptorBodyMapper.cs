using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingSourceDescriptorBodyMapper
{
    internal static bool TryMap(
        LinkingSourceDescriptorBody? body,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;
        violation = null!;

        if (body is null)
        {
            violation = LinkingBodyViolations.Malformed("body");
            return false;
        }

        if (!LinkingSourceTokens.TryParseKind(body.Kind, out var kind))
        {
            violation = LinkingBodyViolations.Malformed("kind", body.Kind);
            return false;
        }

        if (LinkingSourceDescriptorValidation.RequiredTextError(body.Label, "label") is not null)
        {
            violation = LinkingBodyViolations.Malformed("label", body.Label);
            return false;
        }

        var label = body.Label!;

        return kind switch
        {
            LinkingSourceKind.Root => TryMapDimension(
                body.RootId, "rootId", id => new LinkingSourceDescriptor.Root(id, label),
                out descriptor, out violation),
            LinkingSourceKind.Lemma => TryMapTypedDimension(
                body.LemmaId, "lemmaId", body.TypeCode,
                (id, typeCode) => new LinkingSourceDescriptor.Lemma(id, typeCode, label),
                out descriptor, out violation),
            LinkingSourceKind.Stem => TryMapTypedDimension(
                body.StemId, "stemId", body.TypeCode,
                (id, typeCode) => new LinkingSourceDescriptor.Stem(id, typeCode, label),
                out descriptor, out violation),
            LinkingSourceKind.UniqueWord => TryMapUniqueWord(body, label, out descriptor, out violation),
            LinkingSourceKind.WordType => TryMapWordType(body.Selection, label, out descriptor, out violation),
            LinkingSourceKind.ManualMushafAyahs => TryMapManual(
                body.ManualAyahs, label, out descriptor, out violation),
            _ => Reject("kind", body.Kind, out violation),
        };
    }

    private static bool TryMapDimension(
        int? id,
        string field,
        Func<int, LinkingSourceDescriptor> create,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;

        if (!LinkingBodyViolations.TryIdentifier(id, field, out violation))
        {
            return false;
        }

        descriptor = create(id!.Value);
        return true;
    }

    private static bool TryMapTypedDimension(
        int? id,
        string field,
        string? typeCode,
        Func<int, string?, LinkingSourceDescriptor> create,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;

        if (!LinkingBodyViolations.TryIdentifier(id, field, out violation))
        {
            return false;
        }

        if (LinkingSourceDescriptorValidation.OptionalTextError(typeCode, "typeCode") is not null)
        {
            violation = LinkingBodyViolations.Malformed("typeCode", typeCode);
            return false;
        }

        descriptor = create(id!.Value, typeCode);
        return true;
    }

    private static bool TryMapUniqueWord(
        LinkingSourceDescriptorBody body,
        string label,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;
        violation = null!;

        if (!LinkingSourceTokens.TryParseUniqueWordMode(body.Mode, out var mode))
        {
            violation = LinkingBodyViolations.Malformed("mode", body.Mode);
            return false;
        }

        if (!LinkingBodyViolations.TryIdentifier(body.WordId, "wordId", out violation))
        {
            return false;
        }

        descriptor = new LinkingSourceDescriptor.UniqueWord(mode, body.WordId!.Value, label);
        return true;
    }

    private static bool TryMapWordType(
        LinkingWordTypeSelectionBody? selection,
        string label,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;

        if (!LinkingWordTypeSelectionBodyMapper.TryMap(selection, out var mapped, out violation))
        {
            return false;
        }

        descriptor = new LinkingSourceDescriptor.WordType(mapped, label);
        return true;
    }

    private static bool TryMapManual(
        IReadOnlyList<LinkingManualAyahBody>? manualAyahs,
        string label,
        out LinkingSourceDescriptor descriptor,
        out LinkingDescriptorViolation violation)
    {
        descriptor = null!;
        violation = null!;

        if (manualAyahs is null || manualAyahs.Count == 0)
        {
            violation = LinkingBodyViolations.Malformed(LinkingManualAyahCompleteness.ManualAyahsField);
            return false;
        }

        var verseKeys = new List<VerseKey>(manualAyahs.Count);

        foreach (var manualAyah in manualAyahs)
        {
            var rawVerseKey = manualAyah?.VerseKey;

            if (LinkingSourceDescriptorValidation.VerseKeyError(rawVerseKey) is not null)
            {
                violation = LinkingBodyViolations.Malformed("manualAyahs.verseKey", rawVerseKey);
                return false;
            }

            verseKeys.Add(new VerseKey(rawVerseKey!));
        }

        descriptor = new LinkingSourceDescriptor.ManualMushafAyahs(verseKeys, label);
        return true;
    }

    private static bool Reject(string field, string? value, out LinkingDescriptorViolation violation)
    {
        violation = LinkingBodyViolations.Malformed(field, value);
        return false;
    }
}
