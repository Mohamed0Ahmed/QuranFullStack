using System.Text.RegularExpressions;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal static partial class QuranTopicsBookSourceValidator
{
    internal static (QuranTopicsBookMetrics Metrics, List<string> Checks, List<string> Warnings, List<string> Errors)
        Validate(QuranTopicsBookDocument document)
    {
        var checks = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();
        if (document.Source is null || document.Policy is null || document.Sections is null)
        {
            errors.Add("source, policy, and sections are required");
            return (new QuranTopicsBookMetrics(0, 0, 0, 0, 0, 0, 0, 0), checks, warnings, errors);
        }

        ValidateHeader(document, checks, errors);
        if (document.Sections.Count == 0)
        {
            errors.Add("at least one section is required");
        }

        RequireUniquePositive(document.Sections.Select(section => section.Order), "section order", errors);
        RequireUniqueKeys(document.Sections.Select(section => section.Key), "section", errors);
        RequireNonBlank(document.Sections.Select(section => section.Name), "section name", errors);
        var allDoors = document.Sections.SelectMany(section => section.Doors.Select(door => (section, door))).ToList();
        RequireUniqueKeys(allDoors.Select(item => item.door.Key), "door", errors);
        RequireNonBlank(allDoors.Select(item => item.door.Name), "door name", errors);
        var doorsByKey = allDoors
            .GroupBy(item => item.door.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var section in document.Sections)
        {
            ValidateSection(document, section, doorsByKey, errors);
        }

        var duplicateGlobalOrder = allDoors
            .Where(item => item.door.ParentKey is null)
            .GroupBy(item => item.door.GlobalOrder)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGlobalOrder is not null)
        {
            errors.Add($"duplicate root globalOrder '{duplicateGlobalOrder.Key}'");
        }

        ValidateAcyclic(doorsByKey, errors);
        if (errors.Count == 0)
        {
            checks.Add("hierarchy");
            checks.Add("ayah-groups");
        }

        return (BuildMetrics(document, allDoors), checks, warnings, errors);
    }

    private static void ValidateHeader(
        QuranTopicsBookDocument document,
        ICollection<string> checks,
        ICollection<string> errors)
    {
        if (document.Format != QuranTopicsBookContract.Format
            || document.FormatVersion != QuranTopicsBookContract.FormatVersion)
        {
            errors.Add($"format must be {QuranTopicsBookContract.Format} v{QuranTopicsBookContract.FormatVersion}");
        }
        else
        {
            checks.Add("format-v1");
        }

        if (string.IsNullOrWhiteSpace(document.Title)
            || string.IsNullOrWhiteSpace(document.Source.FileName)
            || document.Source.FileName.Length > 128
            || string.IsNullOrWhiteSpace(document.Source.Sha256)
            || !Sha256Regex().IsMatch(document.Source.Sha256)
            || document.Source.PdfPageFrom <= 0
            || document.Source.PdfPageTo < document.Source.PdfPageFrom)
        {
            errors.Add("source provenance is incomplete or invalid");
        }
        else
        {
            checks.Add("source-provenance");
        }

        if (document.Policy.ParentAyahPolicy != QuranTopicsBookContract.DirectOnlyParentAyahPolicy
            || document.Policy.GroupingPolicy != QuranTopicsBookContract.ConsecutiveRangesGroupingPolicy)
        {
            errors.Add("import policy does not match direct-only parents and grouped consecutive ranges");
        }
        else
        {
            checks.Add("import-policy");
        }
    }

    private static void ValidateSection(
        QuranTopicsBookDocument document,
        QuranTopicsBookSection section,
        IReadOnlyDictionary<string, (QuranTopicsBookSection section, QuranTopicsBookDoor door)> doorsByKey,
        ICollection<string> errors)
    {
        if (section.Doors.Count == 0)
        {
            errors.Add($"section '{section.Key}' has no doors");
            return;
        }

        foreach (var siblingGroup in section.Doors.GroupBy(door => door.ParentKey, StringComparer.Ordinal))
        {
            RequireUniquePositive(siblingGroup.Select(door => door.Order), $"sibling order in section '{section.Key}'", errors);
            var duplicateName = siblingGroup
                .GroupBy(door => door.Name.Trim(), StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateName is not null)
            {
                errors.Add($"duplicate sibling door name '{duplicateName.Key}' in section '{section.Key}'");
            }
        }

        foreach (var door in section.Doors)
        {
            ValidateDoor(document, section, door, doorsByKey, errors);
        }
    }

    private static void ValidateDoor(
        QuranTopicsBookDocument document,
        QuranTopicsBookSection section,
        QuranTopicsBookDoor door,
        IReadOnlyDictionary<string, (QuranTopicsBookSection section, QuranTopicsBookDoor door)> doorsByKey,
        ICollection<string> errors)
    {
        if (door.ParentKey is null)
        {
            if (door.GlobalOrder is null or <= 0)
            {
                errors.Add($"root door '{door.Key}' requires a positive globalOrder");
            }
        }
        else
        {
            if (door.GlobalOrder is not null)
            {
                errors.Add($"child door '{door.Key}' must not define globalOrder");
            }

            if (!doorsByKey.TryGetValue(door.ParentKey, out var parent)
                || parent.section.Key != section.Key)
            {
                errors.Add($"door '{door.Key}' has a missing or cross-section parent '{door.ParentKey}'");
            }
        }

        if (door.PdfPages.Count == 0
            || door.PdfPages.Any(page => page < document.Source.PdfPageFrom || page > document.Source.PdfPageTo))
        {
            errors.Add($"door '{door.Key}' has an invalid PDF page reference");
        }

        ValidateAyahGroups(door, errors);
    }

    private static void ValidateAyahGroups(QuranTopicsBookDoor door, ICollection<string> errors)
    {
        RequireUniquePositive(door.AyahGroups.Select(group => group.Order), $"ayah-group order for door '{door.Key}'", errors);
        var seenVerseKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in door.AyahGroups)
        {
            var parsed = new List<VerseKey>();
            foreach (var verseKey in group.VerseKeys)
            {
                try
                {
                    parsed.Add(new VerseKey(verseKey));
                }
                catch (ArgumentException)
                {
                    errors.Add($"door '{door.Key}' has invalid verseKey '{verseKey}'");
                }

                if (!seenVerseKeys.Add(verseKey))
                {
                    errors.Add($"door '{door.Key}' repeats verseKey '{verseKey}'");
                }
            }

            var validSingle = group.Kind == QuranTopicsBookContract.SingleGroupKind && parsed.Count == 1;
            var validRange = group.Kind == QuranTopicsBookContract.ConsecutiveRangeGroupKind
                && parsed.Count >= 2
                && parsed.Zip(parsed.Skip(1)).All(pair =>
                    pair.First.Surah == pair.Second.Surah && pair.Second.Ayah == pair.First.Ayah + 1);
            if (!validSingle && !validRange)
            {
                errors.Add($"door '{door.Key}' ayah group {group.Order} is not a valid single or consecutive range");
            }
        }
    }

    private static void ValidateAcyclic(
        IReadOnlyDictionary<string, (QuranTopicsBookSection section, QuranTopicsBookDoor door)> doorsByKey,
        ICollection<string> errors)
    {
        foreach (var key in doorsByKey.Keys)
        {
            var path = new HashSet<string>(StringComparer.Ordinal);
            var current = key;
            while (doorsByKey.TryGetValue(current, out var item) && item.door.ParentKey is not null)
            {
                if (!path.Add(current))
                {
                    errors.Add($"door hierarchy contains a cycle at '{current}'");
                    return;
                }

                current = item.door.ParentKey;
            }
        }
    }

    private static QuranTopicsBookMetrics BuildMetrics(
        QuranTopicsBookDocument document,
        IReadOnlyList<(QuranTopicsBookSection section, QuranTopicsBookDoor door)> allDoors)
    {
        var parentKeys = allDoors
            .Where(item => item.door.ParentKey is not null)
            .Select(item => item.door.ParentKey!)
            .ToHashSet(StringComparer.Ordinal);
        var groups = allDoors.SelectMany(item => item.door.AyahGroups).ToList();
        var verseKeys = groups.SelectMany(group => group.VerseKeys).ToList();
        return new QuranTopicsBookMetrics(
            document.Sections.Count,
            allDoors.Count,
            parentKeys.Count,
            allDoors.Count - parentKeys.Count,
            groups.Count,
            groups.Count(group => group.Kind == QuranTopicsBookContract.ConsecutiveRangeGroupKind),
            verseKeys.Count,
            verseKeys.Distinct(StringComparer.Ordinal).Count());
    }

    private static void RequireUniquePositive(
        IEnumerable<int> values,
        string field,
        ICollection<string> errors)
    {
        var materialized = values.ToList();
        if (materialized.Any(value => value <= 0) || materialized.Distinct().Count() != materialized.Count)
        {
            errors.Add($"{field} values must be positive and unique");
        }
    }

    private static void RequireUniqueKeys(
        IEnumerable<string> keys,
        string field,
        ICollection<string> errors)
    {
        var materialized = keys.ToList();
        if (materialized.Any(key => string.IsNullOrWhiteSpace(key) || key.Length > 128 || !StableKeyRegex().IsMatch(key))
            || materialized.Distinct(StringComparer.Ordinal).Count() != materialized.Count)
        {
            errors.Add($"{field} keys must be unique lowercase ASCII stable keys");
        }
    }

    private static void RequireNonBlank(
        IEnumerable<string> values,
        string field,
        ICollection<string> errors)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"{field} values must be non-blank");
        }
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableKeyRegex();
}
