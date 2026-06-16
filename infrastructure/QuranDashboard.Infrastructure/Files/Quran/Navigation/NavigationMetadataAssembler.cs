using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.Navigation;

public sealed class NavigationMetadataAssembler
{
    public AssembledNavigationMetadata Assemble(
        NavigationMetadataSourceData source,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        NavigationExpectedCounts expected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);
        ArgumentNullException.ThrowIfNull(expected);

        var warnings = new List<NavigationCheckResult>();

        var juzRows = AssembleDivisions(
            source.Juz,
            ayahIdsByVerseKey,
            expected.Ayahs,
            warnings,
            divisionLabel: "juz");
        var hizbRows = AssembleDivisions(
            source.Hizb,
            ayahIdsByVerseKey,
            expected.Ayahs,
            warnings,
            divisionLabel: "hizb");
        var rubRows = AssembleDivisions(
            source.Rub,
            ayahIdsByVerseKey,
            expected.Ayahs,
            warnings,
            divisionLabel: "rub");

        var hizbWithParents = AssignParent(
            hizbRows,
            juzRows,
            (child, parent) => child with { ParentJuzNumber = parent.Number });
        var rubWithParents = AssignParent(
            rubRows,
            hizbRows,
            (child, parent) => child with { ParentHizbNumber = parent.Number });

        var ayahAssignments = BuildAyahAssignments(
            juzRows,
            hizbWithParents,
            rubWithParents,
            ayahIdsByVerseKey,
            expected.Ayahs);
        var sajdaRows = AssembleSajda(source.Sajda, ayahIdsByVerseKey, warnings, expected);

        return new AssembledNavigationMetadata(
            juzRows,
            hizbWithParents,
            rubWithParents,
            sajdaRows,
            ayahAssignments,
            warnings);
    }

    private static IReadOnlyList<AssembledDivision> AssembleDivisions(
        IReadOnlyList<NavigationDivisionDto> divisions,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        int expectedAyahCount,
        List<NavigationCheckResult> warnings,
        string divisionLabel)
    {
        var rows = new List<AssembledDivision>(divisions.Count);

        foreach (var division in divisions.OrderBy(item => item.Number))
        {
            var expandedKeys = ExpandVerseMapping(division.VerseMapping);
            var computedCount = (short)expandedKeys.Count;
            var firstAyahId = ResolveAyahId(division.FirstVerseKey, ayahIdsByVerseKey);
            var lastAyahId = ResolveAyahId(division.LastVerseKey, ayahIdsByVerseKey);

            if (division.VersesCount != computedCount)
            {
                warnings.Add(NavigationValidationChecks.Warning(
                    NavigationMetadataInvariants.WarningVerseCountMatch,
                    $"{divisionLabel} {division.Number}: source={division.VersesCount}",
                    $"computed={computedCount}",
                    passed: false));
            }

            rows.Add(new AssembledDivision(
                division.Number,
                computedCount,
                firstAyahId,
                lastAyahId,
                division.FirstVerseKey,
                division.LastVerseKey,
                expandedKeys,
                ParentJuzNumber: null,
                ParentHizbNumber: null));
        }

        ValidateCoverage(rows, expectedAyahCount, divisionLabel);
        return rows;
    }

    private static IReadOnlyList<AssembledSajda> AssembleSajda(
        IReadOnlyList<NavigationSajdaDto> sajdas,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        List<NavigationCheckResult> warnings,
        NavigationExpectedCounts expected)
    {
        var rows = sajdas
            .OrderBy(item => item.SajdahNumber)
            .Select(sajda => new AssembledSajda(
                sajda.SajdahNumber,
                ResolveAyahId(sajda.VerseKey, ayahIdsByVerseKey),
                sajda.VerseKey,
                ParseSajdaType(sajda.SajdahType)))
            .ToList();

        var required = rows.Count(row => row.SajdahType == SajdahType.Required);
        var optional = rows.Count(row => row.SajdahType == SajdahType.Optional);

        if (expected.Sajda == NavigationMetadataInvariants.ExpectedSajda
            && (required != NavigationMetadataInvariants.ExpectedSajdaRequired
                || optional != NavigationMetadataInvariants.ExpectedSajdaOptional))
        {
            warnings.Add(NavigationValidationChecks.Warning(
                NavigationMetadataInvariants.WarningSajdaDistribution,
                $"optional={NavigationMetadataInvariants.ExpectedSajdaOptional}, required={NavigationMetadataInvariants.ExpectedSajdaRequired}",
                $"optional={optional}, required={required}",
                passed: false));
        }

        return rows;
    }

    private static SajdahType ParseSajdaType(string value) =>
        string.Equals(value, "required", StringComparison.Ordinal)
            ? SajdahType.Required
            : SajdahType.Optional;

    private static IReadOnlyList<AssembledDivision> AssignParent(
        IReadOnlyList<AssembledDivision> children,
        IReadOnlyList<AssembledDivision> parents,
        Func<AssembledDivision, AssembledDivision, AssembledDivision> assignParent)
    {
        var updated = new List<AssembledDivision>(children.Count);

        foreach (var child in children)
        {
            var matches = parents
                .Where(parent =>
                    parent.FirstAyahId <= child.FirstAyahId
                    && child.LastAyahId <= parent.LastAyahId)
                .ToList();

            if (matches.Count != 1)
            {
                var check = NavigationValidationChecks.Hard(
                    NavigationMetadataInvariants.CheckHierarchy,
                    "exactly one parent by range containment",
                    $"{child.Number}: matches={matches.Count}",
                    false);
                NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
            }

            updated.Add(assignParent(child, matches[0]));
        }

        return updated;
    }

    private static Dictionary<int, AyahNavigationAssignment> BuildAyahAssignments(
        IReadOnlyList<AssembledDivision> juzRows,
        IReadOnlyList<AssembledDivision> hizbRows,
        IReadOnlyList<AssembledDivision> rubRows,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        int expectedAyahCount)
    {
        var assignments = new Dictionary<int, AyahNavigationAssignment>();

        ApplyDivisionAssignments(juzRows, assignments, ayahIdsByVerseKey, (assignment, number) =>
            assignment with { JuzNumber = number });
        ApplyDivisionAssignments(hizbRows, assignments, ayahIdsByVerseKey, (assignment, number) =>
            assignment with { HizbNumber = number });
        ApplyDivisionAssignments(rubRows, assignments, ayahIdsByVerseKey, (assignment, number) =>
            assignment with { RubNumber = number });

        if (assignments.Count != expectedAyahCount)
        {
            var check = NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckAyahColumnsComplete,
                expectedAyahCount.ToString(CultureInfo.InvariantCulture),
                assignments.Count.ToString(CultureInfo.InvariantCulture),
                false);
            NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
        }

        return assignments;
    }

    private static void ApplyDivisionAssignments(
        IReadOnlyList<AssembledDivision> divisions,
        Dictionary<int, AyahNavigationAssignment> assignments,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        Func<AyahNavigationAssignment, short, AyahNavigationAssignment> assignNumber)
    {
        foreach (var division in divisions)
        {
            foreach (var verseKey in division.ExpandedVerseKeys)
            {
                var ayahId = ResolveAyahId(verseKey, ayahIdsByVerseKey);
                if (!assignments.TryGetValue(ayahId, out var assignment))
                {
                    assignment = new AyahNavigationAssignment(ayahId, 0, 0, 0);
                }

                assignments[ayahId] = assignNumber(assignment, division.Number);
            }
        }
    }

    private static void ValidateCoverage(
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

        if (overlap.Count > 0)
        {
            var check = NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
                "no overlaps",
                $"{divisionLabel}: {string.Join(", ", overlap.Take(5))}",
                false);
            NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
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

            var check = NavigationValidationChecks.Hard(
                checkId,
                $"{expectedAyahCount} once",
                $"{allKeys.Count} once",
                false);
            NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
        }
    }

    public static IReadOnlyList<string> ExpandVerseMapping(IReadOnlyDictionary<string, string> verseMapping)
    {
        var keys = new List<string>();

        foreach (var (surahText, rangeText) in verseMapping.OrderBy(pair => int.Parse(pair.Key, CultureInfo.InvariantCulture)))
        {
            var parts = rangeText.Split('-', 2);
            var from = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var to = int.Parse(parts[1], CultureInfo.InvariantCulture);

            for (var ayah = from; ayah <= to; ayah++)
            {
                keys.Add($"{surahText}:{ayah}");
            }
        }

        return keys;
    }

    private static int ResolveAyahId(string verseKey, IReadOnlyDictionary<string, int> ayahIdsByVerseKey)
    {
        if (!ayahIdsByVerseKey.TryGetValue(verseKey, out var ayahId))
        {
            var check = NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckVerseKeysResolve,
                verseKey,
                "unresolved",
                false);
            NavigationValidationChecks.EnsureAllHardChecksPassed([check]);
        }

        return ayahId;
    }
}

public sealed record AssembledNavigationMetadata(
    IReadOnlyList<AssembledDivision> Juz,
    IReadOnlyList<AssembledDivision> Hizb,
    IReadOnlyList<AssembledDivision> Rub,
    IReadOnlyList<AssembledSajda> Sajda,
    IReadOnlyDictionary<int, AyahNavigationAssignment> AyahAssignments,
    IReadOnlyList<NavigationCheckResult> Warnings);

public sealed record AssembledDivision(
    short Number,
    short VersesCount,
    int FirstAyahId,
    int LastAyahId,
    string FirstVerseKey,
    string LastVerseKey,
    IReadOnlyList<string> ExpandedVerseKeys,
    short? ParentJuzNumber,
    short? ParentHizbNumber);

public sealed record AssembledSajda(
    short SajdahNumber,
    int AyahId,
    string VerseKey,
    SajdahType SajdahType);

public sealed record AyahNavigationAssignment(
    int AyahId,
    short JuzNumber,
    short HizbNumber,
    short RubNumber);
