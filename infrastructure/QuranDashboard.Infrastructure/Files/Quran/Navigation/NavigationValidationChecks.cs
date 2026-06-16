using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.Navigation;

internal static class NavigationValidationChecks
{
    public static NavigationCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, NavigationImportConstants.HardSeverity, expected, observed, passed);

    public static NavigationCheckResult Warning(string id, string expected, string observed, bool passed) =>
        new(id, NavigationImportConstants.WarningSeverity, expected, observed, passed);

    public static NavigationCheckResult ValidateSourceCounts(
        int juzCount,
        int hizbCount,
        int rubCount,
        int sajdaCount,
        NavigationExpectedCounts expected)
    {
        var observed = $"{juzCount}/{hizbCount}/{rubCount}/{sajdaCount}";
        var expectedText = $"{expected.Juz}/{expected.Hizb}/{expected.Rub}/{expected.Sajda}";
        var passed = juzCount == expected.Juz
            && hizbCount == expected.Hizb
            && rubCount == expected.Rub
            && sajdaCount == expected.Sajda;

        return Hard(NavigationMetadataInvariants.CheckSourceCount, expectedText, observed, passed);
    }

    public static NavigationCheckResult ValidateSajdaTypeAllowed(string sajdaType, string fileContext)
    {
        var passed = string.Equals(sajdaType, "required", StringComparison.Ordinal)
            || string.Equals(sajdaType, "optional", StringComparison.Ordinal);

        return Hard(
            NavigationMetadataInvariants.CheckSajdaType,
            "required|optional",
            $"{fileContext}:{sajdaType}",
            passed);
    }

    public static NavigationCheckResult ValidateVerseKeyResolves(
        string verseKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey)
    {
        var passed = ayahIdsByVerseKey.ContainsKey(verseKey);
        return Hard(
            NavigationMetadataInvariants.CheckVerseKeysResolve,
            verseKey,
            passed ? verseKey : "unresolved",
            passed);
    }

    public static IReadOnlyList<NavigationCheckResult> ValidateRangeCoverage(
        IReadOnlyList<AssembledDivision> rows,
        int expectedAyahCount,
        string divisionLabel)
    {
        var allKeys = new HashSet<string>(StringComparer.Ordinal);
        var overlap = new List<string>();

        foreach (var row in rows)
        {
            foreach (var key in row.ExpandedVerseKeys)
            {
                if (!allKeys.Add(key))
                {
                    overlap.Add(key);
                }
            }
        }

        var checks = new List<NavigationCheckResult>();

        if (overlap.Count > 0)
        {
            checks.Add(Hard(
                NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
                "no overlaps",
                $"{divisionLabel}: {string.Join(", ", overlap.Take(5))}",
                false));
        }

        if (allKeys.Count != expectedAyahCount)
        {
            var checkId = divisionLabel switch
            {
                "juz" => NavigationMetadataInvariants.CheckRangeCoverageJuz,
                "hizb" => NavigationMetadataInvariants.CheckRangeCoverageHizb,
                "rub" => NavigationMetadataInvariants.CheckRangeCoverageRub,
                _ => NavigationMetadataInvariants.CheckNoRangeGapsOverlaps
            };

            checks.Add(Hard(
                checkId,
                $"{expectedAyahCount} once",
                $"{allKeys.Count} once",
                false));
        }

        return checks;
    }

    public static NavigationCheckResult ValidateHierarchyContainment(
        short childNumber,
        int parentMatchCount)
    {
        return Hard(
            NavigationMetadataInvariants.CheckHierarchy,
            "exactly one parent by range containment",
            $"{childNumber}: matches={parentMatchCount}",
            parentMatchCount == 1);
    }

    public static NavigationCheckResult ValidateAyahColumnsComplete(
        int taggedAyahCount,
        int totalAyahCount,
        int expectedAyahCount)
    {
        var passed = taggedAyahCount == expectedAyahCount && totalAyahCount == expectedAyahCount;
        return Hard(
            NavigationMetadataInvariants.CheckAyahColumnsComplete,
            expectedAyahCount.ToString(CultureInfo.InvariantCulture),
            taggedAyahCount.ToString(CultureInfo.InvariantCulture),
            passed);
    }

    public static NavigationCheckResult ValidateContiguousNumbers(
        IEnumerable<short> numbers,
        int expectedCount,
        string label)
    {
        var sorted = numbers.OrderBy(number => number).ToList();
        var expected = Enumerable.Range(1, expectedCount).Select(index => (short)index).ToList();
        var observed = sorted.Count == 0 ? "none" : string.Join(",", sorted);
        var passed = sorted.Count == expectedCount && sorted.SequenceEqual(expected);

        return Hard(
            NavigationMetadataInvariants.CheckJsonShape,
            $"{label} contiguous 1..{expectedCount.ToString(CultureInfo.InvariantCulture)}",
            observed,
            passed);
    }

    public static IReadOnlyList<NavigationCheckResult> ValidateSourceShape(
        NavigationMetadataSourceData source,
        NavigationExpectedCounts expected)
    {
        var checks = new List<NavigationCheckResult>
        {
            ValidateContiguousNumbers(source.Juz.Select(division => division.Number), expected.Juz, "juz"),
            ValidateContiguousNumbers(source.Hizb.Select(division => division.Number), expected.Hizb, "hizb"),
            ValidateContiguousNumbers(source.Rub.Select(division => division.Number), expected.Rub, "rub"),
            ValidateContiguousNumbers(source.Sajda.Select(sajda => sajda.SajdahNumber), expected.Sajda, "sajda")
        };

        foreach (var sajda in source.Sajda)
        {
            checks.Add(ValidateSajdaTypeAllowed(sajda.SajdahType, $"sajda {sajda.SajdahNumber}"));
        }

        return checks;
    }

    public static bool TryParsePositiveShort(int value, out short parsed)
    {
        if (value is < 1 or > short.MaxValue)
        {
            parsed = 0;
            return false;
        }

        parsed = (short)value;
        return true;
    }

    public static void EnsureAllHardChecksPassed(IReadOnlyList<NavigationCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == NavigationImportConstants.HardSeverity
                && !check.Passed)
            .ToList();

        if (failed.Count > 0)
        {
            throw new NavigationMetadataValidationException(failed);
        }
    }
}
