using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

public sealed class TafsirAssembler
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);

    public TafsirSourceData Assemble(
        TafsirPackageManifest manifest,
        IReadOnlyDictionary<string, ParsedTafsirSourceFile> parsedSourcesByKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(parsedSourcesByKey);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);
        ArgumentNullException.ThrowIfNull(ayahTextsByVerseKey);

        var sources = new List<TafsirSourceDto>();
        var entries = new List<TafsirEntryDto>();
        var ayahEntries = new List<TafsirAyahEntryDto>();
        var seenSourceAyah = new HashSet<(string SourceKey, int AyahId)>();

        foreach (var manifestSource in manifest.ApprovedSources)
        {
            if (!parsedSourcesByKey.TryGetValue(manifestSource.SourceKey, out var parsedSource))
            {
                throw new InvalidDataException(
                    $"Parsed source file for '{manifestSource.SourceKey}' was not loaded.");
            }

            var assembled = AssembleSource(
                manifestSource,
                parsedSource,
                manifest.ManifestJson,
                ayahIdsByVerseKey,
                ayahTextsByVerseKey,
                seenSourceAyah);

            sources.Add(assembled.Source);
            entries.AddRange(assembled.Entries);
            ayahEntries.AddRange(assembled.AyahEntries);
        }

        return new TafsirSourceData(
            sources,
            entries,
            ayahEntries,
            manifest.ExcludedSources);
    }

    public TafsirPerSourceAssembly AssembleSource(
        TafsirManifestSourceRecord manifestSource,
        ParsedTafsirSourceFile parsedSource,
        string manifestJson,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey,
        HashSet<(string SourceKey, int AyahId)> seenSourceAyah)
    {
        ArgumentNullException.ThrowIfNull(manifestSource);
        ArgumentNullException.ThrowIfNull(parsedSource);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);
        ArgumentNullException.ThrowIfNull(ayahTextsByVerseKey);
        ArgumentNullException.ThrowIfNull(seenSourceAyah);

        var sourceKey = manifestSource.SourceKey;
        var textBlocks = BuildTextBlocks(
            sourceKey,
            parsedSource,
            ayahIdsByVerseKey,
            ayahTextsByVerseKey);

        return new TafsirPerSourceAssembly(
            MapSourceDto(manifestSource, manifestJson),
            textBlocks.Entries,
            ExpandAyahEntries(
                sourceKey,
                parsedSource,
                textBlocks,
                ayahIdsByVerseKey,
                seenSourceAyah));
    }

    private static TafsirSourceDto MapSourceDto(
        TafsirManifestSourceRecord manifestSource,
        string manifestJson) =>
        new(
            manifestSource.SourceKey,
            manifestSource.LanguageCode,
            manifestSource.LanguageNameAr,
            manifestSource.LanguageNameEn,
            manifestSource.Direction,
            manifestSource.DisplayNameAr,
            manifestSource.ShortNameAr,
            manifestSource.DisplayNameEn,
            manifestSource.ShortNameEn,
            manifestSource.ContributorKey,
            manifestSource.ContributorNameAr,
            manifestSource.ContributorNameEn,
            manifestSource.ContributorType,
            manifestSource.ResourceKind,
            manifestSource.TafsirKind,
            manifestSource.ContentCoverageCount,
            manifestSource.PackageFile.Replace('\\', '/'),
            manifestSource.SourceFileOriginal,
            manifestSource.Sha256,
            manifestSource.FileSizeBytes,
            manifestSource.License ?? "unknown",
            manifestSource.Provenance ?? "unknown",
            manifestJson);

    private static TextBlockAssembly BuildTextBlocks(
        string sourceKey,
        ParsedTafsirSourceFile parsedSource,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        var leaders = new Dictionary<string, ResolvedTextBlock>(StringComparer.Ordinal);
        var entries = new List<TafsirEntryDto>();

        foreach (var (verseKey, entry) in parsedSource.Entries)
        {
            if (entry is not ParsedTafsirSourceEntry.TextOwning textOwning)
            {
                continue;
            }

            ValidateVerseKeyFormat(verseKey);
            var leaderAyahId = ResolveAyahId(verseKey, ayahIdsByVerseKey, sourceKey);
            EnsureNotQuranText(textOwning.Text, verseKey, ayahTextsByVerseKey);

            var coveredKeys = (textOwning.AyahKeys is { Length: > 0 } keys
                ? keys
                : [verseKey]).Distinct(StringComparer.Ordinal).ToArray();

            foreach (var coveredKey in coveredKeys)
            {
                ValidateVerseKeyFormat(coveredKey);
                _ = ResolveAyahId(coveredKey, ayahIdsByVerseKey, sourceKey);
            }

            var sourceShape = coveredKeys.Length > 1
                ? TafsirImportConstants.SourceShapeGroupedLeader
                : TafsirImportConstants.SourceShapeFlat;

            var coveredAyahKeysJson = JsonSerializer.Serialize(coveredKeys);
            var textHash = ComputeTextHash(textOwning.Text);

            leaders[verseKey] = new ResolvedTextBlock(
                verseKey,
                leaderAyahId,
                textOwning.Text,
                coveredKeys,
                sourceShape,
                textHash);

            entries.Add(new TafsirEntryDto(
                sourceKey,
                verseKey,
                leaderAyahId,
                textOwning.Text,
                (short)coveredKeys.Length,
                coveredAyahKeysJson,
                sourceShape,
                textHash));
        }

        return new TextBlockAssembly(leaders, entries);
    }

    private static IReadOnlyList<TafsirAyahEntryDto> ExpandAyahEntries(
        string sourceKey,
        ParsedTafsirSourceFile parsedSource,
        TextBlockAssembly textBlocks,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        HashSet<(string SourceKey, int AyahId)> seenSourceAyah)
    {
        var ayahEntries = new List<TafsirAyahEntryDto>();

        foreach (var (verseKey, entry) in parsedSource.Entries)
        {
            ValidateVerseKeyFormat(verseKey);

            var resolved = ResolveEntry(
                verseKey,
                entry,
                parsedSource,
                textBlocks.Leaders,
                ayahIdsByVerseKey,
                sourceKey);

            if (!seenSourceAyah.Add((sourceKey, resolved.AyahId)))
            {
                throw new InvalidDataException(
                    $"Duplicate source/ayah mapping for source '{sourceKey}' and ayah id {resolved.AyahId}.");
            }

            ayahEntries.Add(new TafsirAyahEntryDto(
                sourceKey,
                resolved.AyahId,
                verseKey,
                resolved.ValueKind,
                resolved.LeaderVerseKey,
                resolved.IsGroupLeader,
                resolved.AyahId));
        }

        return ayahEntries;
    }

    private static ResolvedAyahEntry ResolveEntry(
        string verseKey,
        ParsedTafsirSourceEntry entry,
        ParsedTafsirSourceFile parsedSource,
        IReadOnlyDictionary<string, ResolvedTextBlock> leaders,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        return entry switch
        {
            ParsedTafsirSourceEntry.TextOwning textOwning => ResolveTextOwningEntry(
                verseKey,
                textOwning,
                leaders,
                ayahIdsByVerseKey,
                sourceKey),
            ParsedTafsirSourceEntry.Pointer pointer => ResolvePointerEntry(
                pointer,
                parsedSource,
                leaders,
                ayahIdsByVerseKey,
                sourceKey),
            _ => throw new InvalidDataException($"Unsupported parsed entry for verse key '{verseKey}'.")
        };
    }

    private static ResolvedAyahEntry ResolveTextOwningEntry(
        string verseKey,
        ParsedTafsirSourceEntry.TextOwning textOwning,
        IReadOnlyDictionary<string, ResolvedTextBlock> leaders,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        if (!leaders.TryGetValue(verseKey, out var leader))
        {
            throw new InvalidDataException(
                $"Text block for verse key '{verseKey}' was not assembled for source '{sourceKey}'.");
        }

        var coveredKeys = textOwning.AyahKeys is { Length: > 0 } keys
            ? keys
            : [verseKey];

        var valueKind = coveredKeys.Length > 1
            ? TafsirImportConstants.ValueKindLeader
            : TafsirImportConstants.ValueKindFlat;

        return new ResolvedAyahEntry(
            ResolveAyahId(verseKey, ayahIdsByVerseKey, sourceKey),
            valueKind,
            verseKey,
            valueKind == TafsirImportConstants.ValueKindLeader);
    }

    private static ResolvedAyahEntry ResolvePointerEntry(
        ParsedTafsirSourceEntry.Pointer pointer,
        ParsedTafsirSourceFile parsedSource,
        IReadOnlyDictionary<string, ResolvedTextBlock> leaders,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        ValidateVerseKeyFormat(pointer.LeaderVerseKey);

        if (!parsedSource.Entries.ContainsKey(pointer.LeaderVerseKey))
        {
            throw new InvalidDataException(
                $"Pointer target '{pointer.LeaderVerseKey}' does not exist in source '{sourceKey}'.");
        }

        if (!leaders.ContainsKey(pointer.LeaderVerseKey))
        {
            throw new InvalidDataException(
                $"Pointer target '{pointer.LeaderVerseKey}' does not own text in source '{sourceKey}'.");
        }

        return new ResolvedAyahEntry(
            ResolveAyahId(pointer.MemberVerseKey, ayahIdsByVerseKey, sourceKey),
            TafsirImportConstants.ValueKindMemberPointer,
            pointer.LeaderVerseKey,
            IsGroupLeader: false);
    }

    private static int ResolveAyahId(
        string verseKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        if (!ayahIdsByVerseKey.TryGetValue(verseKey, out var ayahId))
        {
            throw new InvalidDataException(
                $"Unresolved verse key '{verseKey}' for source '{sourceKey}'.");
        }

        return ayahId;
    }

    private static void EnsureNotQuranText(
        string tafsirText,
        string verseKey,
        IReadOnlyDictionary<string, string> ayahTextsByVerseKey)
    {
        if (ayahTextsByVerseKey.TryGetValue(verseKey, out var ayahText)
            && string.Equals(tafsirText, ayahText, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Tafsir text for verse key '{verseKey}' matches Quran ayah text and must not be copied.");
        }
    }

    private static void ValidateVerseKeyFormat(string verseKey)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            throw new InvalidDataException($"Invalid verse key format '{verseKey}'.");
        }
    }

    internal static string ComputeTextHash(string text) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

    private sealed record TextBlockAssembly(
        IReadOnlyDictionary<string, ResolvedTextBlock> Leaders,
        IReadOnlyList<TafsirEntryDto> Entries);

    private sealed record ResolvedTextBlock(
        string LeaderVerseKey,
        int LeaderAyahId,
        string Text,
        string[] CoveredAyahKeys,
        string SourceShape,
        string TextHash);

    private sealed record ResolvedAyahEntry(
        int AyahId,
        string ValueKind,
        string LeaderVerseKey,
        bool IsGroupLeader);
}

public sealed record TafsirPerSourceAssembly(
    TafsirSourceDto Source,
    IReadOnlyList<TafsirEntryDto> Entries,
    IReadOnlyList<TafsirAyahEntryDto> AyahEntries);
