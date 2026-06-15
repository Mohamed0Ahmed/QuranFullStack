using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.Translations;

public sealed class TranslationAssembler
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);
    private static readonly Regex InlineFootnotePattern = new(@"\[\[.+?\]\]", RegexOptions.Compiled);
    private static readonly Regex HtmlTagPattern = new(@"<[^>]+>", RegexOptions.Compiled);

    public TranslationPerSourceAssembly AssembleSource(
        TranslationManifestSourceRecord manifestSource,
        TranslationDisplayMetadataRecord displayMetadata,
        ParsedTranslationSourceFile parsedSource,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey,
        HashSet<(string SourceKey, int AyahId)> seenSourceAyah,
        int expectedAyahsPerSource)
    {
        ArgumentNullException.ThrowIfNull(manifestSource);
        ArgumentNullException.ThrowIfNull(displayMetadata);
        ArgumentNullException.ThrowIfNull(parsedSource);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);
        ArgumentNullException.ThrowIfNull(ayahTextsByVerseKey);
        ArgumentNullException.ThrowIfNull(seenSourceAyah);

        ValidateJsonShape(manifestSource.SourceKey, parsedSource, expectedAyahsPerSource);

        var sourceKey = manifestSource.SourceKey;
        var containsInlineFootnotes = false;
        var containsHtmlMarkup = false;
        var ayahEntries = new List<TranslationAyahEntryDto>(parsedSource.Entries.Count);

        foreach (var (verseKey, text) in parsedSource.Entries)
        {
            ValidateVerseKeyFormat(verseKey);
            var ayahId = ResolveAyahId(verseKey, ayahIdsByVerseKey, sourceKey);
            EnsureNotQuranText(text, verseKey, ayahTextsByVerseKey);

            if (InlineFootnotePattern.IsMatch(text))
            {
                containsInlineFootnotes = true;
            }

            if (HtmlTagPattern.IsMatch(text))
            {
                containsHtmlMarkup = true;
            }

            if (!seenSourceAyah.Add((sourceKey, ayahId)))
            {
                FailDuplicateMapping(sourceKey, ayahId);
            }

            ayahEntries.Add(new TranslationAyahEntryDto(sourceKey, ayahId, verseKey, text));
        }

        var translationType = displayMetadata.TranslationType;
        if (containsInlineFootnotes && translationType == "simple")
        {
            translationType = "with_footnotes";
        }

        var sourceDto = new TranslationSourceDto(
            sourceKey,
            displayMetadata.LanguageCode,
            displayMetadata.LanguageNameEn,
            displayMetadata.LanguageNameAr,
            displayMetadata.NativeName,
            displayMetadata.Direction,
            translationType,
            displayMetadata.DisplayNameEn,
            displayMetadata.DisplayNameAr,
            displayMetadata.TranslatorKey,
            displayMetadata.TranslatorNameEn,
            displayMetadata.TranslatorNameAr,
            containsInlineFootnotes,
            containsHtmlMarkup,
            manifestSource.ContentCoverageCount,
            manifestSource.PackageFile.Replace('\\', '/'),
            manifestSource.Sha256,
            manifestSource.FileSizeBytes);

        return new TranslationPerSourceAssembly(sourceDto, ayahEntries);
    }

    private static void ValidateJsonShape(
        string sourceKey,
        ParsedTranslationSourceFile parsedSource,
        int expectedAyahsPerSource)
    {
        var observed = parsedSource.Entries.Count.ToString(CultureInfo.InvariantCulture);
        var check = TranslationValidationChecks.Hard(
            TranslationInvariants.CheckJsonShape,
            $"object root with {expectedAyahsPerSource.ToString(CultureInfo.InvariantCulture)} verse keys",
            observed,
            parsedSource.Entries.Count == expectedAyahsPerSource);

        TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
    }

    private static void ValidateVerseKeyFormat(string verseKey)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            throw new InvalidDataException($"Malformed verse key '{verseKey}'.");
        }
    }

    private static int ResolveAyahId(
        string verseKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        if (!ayahIdsByVerseKey.TryGetValue(verseKey, out var ayahId))
        {
            var check = TranslationValidationChecks.Hard(
                TranslationInvariants.CheckAyahKeysResolve,
                verseKey,
                "unresolved",
                false);
            TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
        }

        return ayahId;
    }

    private static void EnsureNotQuranText(
        string text,
        string verseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        if (ayahTextsByVerseKey.TryGetValue(verseKey, out var ayahText)
            && string.Equals(text, ayahText, StringComparison.Ordinal))
        {
            var check = TranslationValidationChecks.Hard(
                TranslationInvariants.CheckNoQuranTextCopy,
                "no copied Quran ayah text",
                $"{verseKey}",
                false);
            TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
        }
    }

    private static void FailDuplicateMapping(string sourceKey, int ayahId)
    {
        var check = TranslationValidationChecks.Hard(
            TranslationInvariants.CheckNoDuplicateAyahEntry,
            "no duplicate source/ayah mapping",
            $"{sourceKey}:{ayahId.ToString(CultureInfo.InvariantCulture)}",
            false);
        TranslationValidationChecks.EnsureAllHardChecksPassed([check]);
    }
}

public sealed record TranslationPerSourceAssembly(
    TranslationSourceDto Source,
    IReadOnlyList<TranslationAyahEntryDto> AyahEntries);
