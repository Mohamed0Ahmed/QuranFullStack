using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingSourceIdentity
{
    private const char PartSeparator = '|';

    public static string For(LinkingSourceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor switch
        {
            LinkingSourceDescriptor.UniqueWord source =>
                Join([
                    KindToken(source.Kind),
                    ModeToken(source.Mode),
                    Number(source.WordId),
                    .. source.TypeCodes,
                ]),
            LinkingSourceDescriptor.Root source =>
                Join([KindToken(source.Kind), Number(source.RootId), .. source.TypeCodes]),
            LinkingSourceDescriptor.Lemma source =>
                DimensionIdentity(source.Kind, source.LemmaId, source.TypeCodes),
            LinkingSourceDescriptor.Stem source =>
                DimensionIdentity(source.Kind, source.StemId, source.TypeCodes),
            LinkingSourceDescriptor.WordType source => WordTypeIdentity(source),
            LinkingSourceDescriptor.ManualMushafAyahs source =>
                ManualMushafAyahsIdentity(source),
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unknown linking source kind."),
        };
    }

    public static byte[] HashFor(LinkingSourceDescriptor descriptor) => HashOf(For(descriptor));

    public static byte[] HashOf(string sourceIdentity)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentity);

        return SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentity));
    }

    private static string WordTypeIdentity(LinkingSourceDescriptor.WordType source)
    {
        var scope = source.Selection.Scope;
        string?[] scopeParts = [scope.Type, scope.ChildCode, scope.Case, scope.Tense, scope.Voice];

        return source.Selection switch
        {
            LinkingWordTypeSelection.Word selection => Join([
                KindToken(source.Kind),
                SelectionToken(selection.Kind),
                Number(selection.TashkeelWordId),
                selection.ContextCode,
                selection.Case,
                selection.Tense,
                selection.Voice,
                .. scopeParts,
            ]),
            LinkingWordTypeSelection.Dimension selection => Join([
                KindToken(source.Kind),
                SelectionToken(selection.Kind),
                Number(selection.DimensionId),
                .. scopeParts,
            ]),
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source.Selection.Kind,
                "Unknown word type selection kind."),
        };
    }

    private static string ManualMushafAyahsIdentity(LinkingSourceDescriptor.ManualMushafAyahs source) =>
        source.ContextKey is null
            ? Join([KindToken(source.Kind), .. source.VerseKeys.Select(verseKey => verseKey.Value)])
            : Join([
                KindToken(source.Kind),
                "context",
                source.ContextKey,
                .. source.VerseKeys.Select(verseKey => verseKey.Value),
            ]);

    private static string DimensionIdentity(
        LinkingSourceKind kind,
        int id,
        IReadOnlyList<string> typeCodes) =>
        typeCodes.Count == 0
            ? Join(KindToken(kind), Number(id), null)
            : Join([KindToken(kind), Number(id), .. typeCodes]);

    private static string KindToken(LinkingSourceKind kind) => LinkingSourceTokens.ToToken(kind);

    private static string ModeToken(LinkingUniqueWordMode mode) => LinkingSourceTokens.ToToken(mode);

    private static string SelectionToken(LinkingWordTypeSelectionKind kind) =>
        LinkingSourceTokens.ToToken(kind);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Join(params string?[] parts) =>
        string.Join(PartSeparator, parts.Select(EncodePart));

    private static string EncodePart(string? part) =>
        Uri.EscapeDataString(part ?? string.Empty)
            .Replace("%21", "!", StringComparison.Ordinal)
            .Replace("%27", "'", StringComparison.Ordinal)
            .Replace("%28", "(", StringComparison.Ordinal)
            .Replace("%29", ")", StringComparison.Ordinal)
            .Replace("%2A", "*", StringComparison.Ordinal);
}
