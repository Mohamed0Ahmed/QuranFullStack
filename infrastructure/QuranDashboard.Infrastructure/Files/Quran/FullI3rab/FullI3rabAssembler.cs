using System.Globalization;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Files.Quran.FullI3rab;

public sealed class FullI3rabAssembler
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);

    public FullI3rabSourceData Assemble(
        FullI3rabPackageManifest manifest,
        IReadOnlyDictionary<string, ParsedFullI3rabSourceFile> parsedSourcesByKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        int expectedAyahsPerSource = FullI3rabInvariants.ExpectedAyahsPerSource)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(parsedSourcesByKey);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);

        var sources = new List<FullI3rabSourceDto>();
        var entries = new List<FullI3rabEntryDto>();
        var ayahEntries = new List<FullI3rabAyahEntryDto>();
        var warnings = new List<string>();
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
                manifest,
                parsedSource,
                ayahIdsByVerseKey,
                seenSourceAyah,
                expectedAyahsPerSource);

            sources.Add(assembled.Source);
            entries.AddRange(assembled.Entries);
            ayahEntries.AddRange(assembled.AyahEntries);

            foreach (var warning in assembled.Warnings)
            {
                warnings.Add($"[{manifestSource.SourceKey}] {warning.Id}: expected {warning.Expected}, observed {warning.Observed}");
            }
        }

        return new FullI3rabSourceData(sources, entries, ayahEntries, warnings);
    }

    public FullI3rabPerSourceAssembly AssembleSource(
        FullI3rabManifestSourceRecord manifestSource,
        FullI3rabPackageManifest manifest,
        ParsedFullI3rabSourceFile parsedSource,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        HashSet<(string SourceKey, int AyahId)> seenSourceAyah,
        int expectedAyahsPerSource = FullI3rabInvariants.ExpectedAyahsPerSource)
    {
        ArgumentNullException.ThrowIfNull(manifestSource);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(parsedSource);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);
        ArgumentNullException.ThrowIfNull(seenSourceAyah);

        ValidateJsonShape(manifestSource.SourceKey, parsedSource, expectedAyahsPerSource);

        var sourceKey = manifestSource.SourceKey;
        var warnings = new List<FullI3rabCheckResult>();
        var textBlocks = BuildTextBlocks(sourceKey, parsedSource, ayahIdsByVerseKey, warnings);

        ValidateBlockPartition(sourceKey, textBlocks, expectedAyahsPerSource);

        return new FullI3rabPerSourceAssembly(
            MapSourceDto(manifestSource, manifest),
            textBlocks.Entries,
            ExpandAyahEntries(
                sourceKey,
                parsedSource,
                textBlocks,
                ayahIdsByVerseKey,
                seenSourceAyah),
            warnings);
    }

    private static void ValidateJsonShape(
        string sourceKey,
        ParsedFullI3rabSourceFile parsedSource,
        int expectedAyahsPerSource)
    {
        var observed = parsedSource.Entries.Count.ToString(CultureInfo.InvariantCulture);
        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckJsonShape,
            expectedAyahsPerSource.ToString(CultureInfo.InvariantCulture),
            observed,
            parsedSource.Entries.Count == expectedAyahsPerSource);

        FullI3rabValidationChecks.EnsureAllHardChecksPassed([check]);
    }

    private static void ValidateBlockPartition(
        string sourceKey,
        TextBlockAssembly textBlocks,
        int expectedAyahsPerSource)
    {
        var allCoveredKeys = textBlocks.Leaders.Values.SelectMany(block => block.CoveredAyahKeys).ToList();
        var distinctCount = allCoveredKeys.Distinct(StringComparer.Ordinal).Count();
        var passed = allCoveredKeys.Count == distinctCount
            && distinctCount == expectedAyahsPerSource;

        var observed = passed
            ? FullI3rabValidationChecks.FormatCount(distinctCount)
            : $"covered={FullI3rabValidationChecks.FormatCount(allCoveredKeys.Count)}, distinct={FullI3rabValidationChecks.FormatCount(distinctCount)}";

        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckBlockPartition,
            $"blocks partition {expectedAyahsPerSource} ayahs with no gaps or overlaps",
            observed,
            passed);

        FullI3rabValidationChecks.EnsureAllHardChecksPassed([check]);
    }

    private static FullI3rabSourceDto MapSourceDto(
        FullI3rabManifestSourceRecord manifestSource,
        FullI3rabPackageManifest manifest) =>
        new(
            manifestSource.SourceKey,
            manifestSource.DisplayNameAr,
            manifestSource.ShortNameAr,
            manifestSource.DisplayNameEn,
            manifestSource.ShortNameEn,
            manifestSource.LanguageCode,
            manifestSource.Direction,
            manifestSource.ContributorNameAr,
            manifestSource.ContributorNameEn,
            FullI3rabImportConstants.ResourceKind,
            manifestSource.MarkupFormat,
            manifestSource.HasQuranQuotationMarkup,
            manifestSource.ContentCoverageCount,
            manifestSource.PackageFile.Replace('\\', '/'),
            manifestSource.SourceFileOriginal,
            manifestSource.Sha256,
            manifestSource.FileSizeBytes,
            manifest.LicenseStatus,
            manifest.ProvenanceStatus,
            manifest.UsageScope,
            manifestSource.ShapeJson);

    private static TextBlockAssembly BuildTextBlocks(
        string sourceKey,
        ParsedFullI3rabSourceFile parsedSource,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        ICollection<FullI3rabCheckResult> warnings)
    {
        var leaders = new Dictionary<string, ResolvedTextBlock>(StringComparer.Ordinal);
        var entries = new List<FullI3rabEntryDto>();

        foreach (var (verseKey, entry) in parsedSource.Entries)
        {
            if (entry is not ParsedFullI3rabSourceEntry.TextOwning textOwning)
            {
                continue;
            }

            ValidateVerseKeyFormat(verseKey);
            var leaderAyahId = ResolveAyahId(verseKey, ayahIdsByVerseKey, sourceKey);
            EnsureNotEmptyHtml(textOwning.I3rabHtml, verseKey, sourceKey);

            var allowlist = FullI3rabValidationChecks.EvaluateHtmlAllowlist(textOwning.I3rabHtml);
            if (!allowlist.Passed)
            {
                warnings.Add(allowlist with { Observed = $"{verseKey}: {allowlist.Observed}" });
            }

            var coveredKeys = (textOwning.AyahKeys is { Length: > 0 } keys
                ? keys
                : [verseKey]).Distinct(StringComparer.Ordinal).ToArray();

            foreach (var coveredKey in coveredKeys)
            {
                ValidateVerseKeyFormat(coveredKey);
                _ = ResolveAyahId(coveredKey, ayahIdsByVerseKey, sourceKey);
            }

            var sourceShape = coveredKeys.Length > 1
                ? FullI3rabImportConstants.SourceShapeGroupedLeader
                : FullI3rabImportConstants.SourceShapeFlat;

            var coveredAyahKeysJson = JsonSerializer.Serialize(coveredKeys);
            var textHash = ComputeTextHash(textOwning.I3rabHtml);

            leaders[verseKey] = new ResolvedTextBlock(
                verseKey,
                leaderAyahId,
                textOwning.I3rabHtml,
                coveredKeys,
                sourceShape,
                textHash);

            entries.Add(new FullI3rabEntryDto(
                sourceKey,
                verseKey,
                leaderAyahId,
                textOwning.I3rabHtml,
                (short)coveredKeys.Length,
                coveredAyahKeysJson,
                sourceShape,
                textHash));
        }

        return new TextBlockAssembly(leaders, entries);
    }

    private static IReadOnlyList<FullI3rabAyahEntryDto> ExpandAyahEntries(
        string sourceKey,
        ParsedFullI3rabSourceFile parsedSource,
        TextBlockAssembly textBlocks,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        HashSet<(string SourceKey, int AyahId)> seenSourceAyah)
    {
        var ayahEntries = new List<FullI3rabAyahEntryDto>();

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
                FailBlockOverlap(sourceKey, resolved.AyahId);
            }

            ayahEntries.Add(new FullI3rabAyahEntryDto(
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
        ParsedFullI3rabSourceEntry entry,
        ParsedFullI3rabSourceFile parsedSource,
        IReadOnlyDictionary<string, ResolvedTextBlock> leaders,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        return entry switch
        {
            ParsedFullI3rabSourceEntry.TextOwning textOwning => ResolveTextOwningEntry(
                verseKey,
                textOwning,
                leaders,
                ayahIdsByVerseKey,
                sourceKey),
            ParsedFullI3rabSourceEntry.Pointer pointer => ResolvePointerEntry(
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
        ParsedFullI3rabSourceEntry.TextOwning textOwning,
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
            ? FullI3rabImportConstants.ValueKindLeader
            : FullI3rabImportConstants.ValueKindFlat;

        return new ResolvedAyahEntry(
            ResolveAyahId(verseKey, ayahIdsByVerseKey, sourceKey),
            valueKind,
            verseKey,
            valueKind == FullI3rabImportConstants.ValueKindLeader);
    }

    private static ResolvedAyahEntry ResolvePointerEntry(
        ParsedFullI3rabSourceEntry.Pointer pointer,
        ParsedFullI3rabSourceFile parsedSource,
        IReadOnlyDictionary<string, ResolvedTextBlock> leaders,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string sourceKey)
    {
        ValidateVerseKeyFormat(pointer.LeaderVerseKey);

        if (!parsedSource.Entries.ContainsKey(pointer.LeaderVerseKey))
        {
            FailPointerResolution(pointer.LeaderVerseKey, sourceKey, "missing target entry");
        }

        if (!leaders.TryGetValue(pointer.LeaderVerseKey, out var leader))
        {
            FailPointerResolution(pointer.LeaderVerseKey, sourceKey, "target does not own text");
        }
        else if (!leader.CoveredAyahKeys.Contains(pointer.MemberVerseKey, StringComparer.Ordinal))
        {
            FailMemberMismatch(pointer.MemberVerseKey, pointer.LeaderVerseKey, sourceKey);
        }

        return new ResolvedAyahEntry(
            ResolveAyahId(pointer.MemberVerseKey, ayahIdsByVerseKey, sourceKey),
            FullI3rabImportConstants.ValueKindMemberPointer,
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
            var check = FullI3rabValidationChecks.Hard(
                FullI3rabInvariants.CheckAyahKeysResolve,
                verseKey,
                "unresolved",
                false);
            throw new FullI3rabValidationException([check]);
        }

        return ayahId;
    }

    private static void EnsureNotEmptyHtml(string i3rabHtml, string verseKey, string sourceKey)
    {
        if (!string.IsNullOrWhiteSpace(i3rabHtml))
        {
            return;
        }

        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckNoEmptyText,
            $"non-empty i'rab HTML for {verseKey}",
            "empty",
            false);
        throw new FullI3rabValidationException([check]);
    }

    private static void FailBlockOverlap(string sourceKey, int ayahId)
    {
        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckBlockPartition,
            $"each ayah covered once per source '{sourceKey}'",
            $"overlap on ayah id {ayahId}",
            false);
        throw new FullI3rabValidationException([check]);
    }

    private static void FailPointerResolution(string leaderVerseKey, string sourceKey, string reason)
    {
        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckPointersResolve,
            $"pointer to text-owning entry in source '{sourceKey}'",
            $"{leaderVerseKey}: {reason}",
            false);
        throw new FullI3rabValidationException([check]);
    }

    private static void FailMemberMismatch(string memberVerseKey, string leaderVerseKey, string sourceKey)
    {
        var check = FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckAyahKeysMemberMatch,
            $"member '{memberVerseKey}' in leader '{leaderVerseKey}' ayah_keys (source '{sourceKey}')",
            $"{memberVerseKey} not in {leaderVerseKey} ayah_keys",
            false);
        throw new FullI3rabValidationException([check]);
    }

    private static void ValidateVerseKeyFormat(string verseKey)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            var check = FullI3rabValidationChecks.Hard(
                FullI3rabInvariants.CheckAyahKeysResolve,
                "valid surah:ayah verse key",
                verseKey,
                false);
            throw new FullI3rabValidationException([check]);
        }
    }

    internal static string ComputeTextHash(string text) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

    private sealed record TextBlockAssembly(
        IReadOnlyDictionary<string, ResolvedTextBlock> Leaders,
        IReadOnlyList<FullI3rabEntryDto> Entries);

    private sealed record ResolvedTextBlock(
        string LeaderVerseKey,
        int LeaderAyahId,
        string I3rabHtml,
        string[] CoveredAyahKeys,
        string SourceShape,
        string TextHash);

    private sealed record ResolvedAyahEntry(
        int AyahId,
        string ValueKind,
        string LeaderVerseKey,
        bool IsGroupLeader);
}

public sealed record FullI3rabPerSourceAssembly(
    FullI3rabSourceDto Source,
    IReadOnlyList<FullI3rabEntryDto> Entries,
    IReadOnlyList<FullI3rabAyahEntryDto> AyahEntries,
    IReadOnlyList<FullI3rabCheckResult> Warnings);
