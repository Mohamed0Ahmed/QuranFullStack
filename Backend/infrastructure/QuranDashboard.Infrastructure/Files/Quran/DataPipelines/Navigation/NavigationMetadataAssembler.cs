using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;

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
        var verseCountMismatches = new List<string>();

        var juzRows = AssembleDivisions(
            source.Juz,
            ayahIdsByVerseKey,
            expected.Ayahs,
            verseCountMismatches,
            divisionLabel: "juz");
        var hizbRows = AssembleDivisions(
            source.Hizb,
            ayahIdsByVerseKey,
            expected.Ayahs,
            verseCountMismatches,
            divisionLabel: "hizb");
        var rubRows = AssembleDivisions(
            source.Rub,
            ayahIdsByVerseKey,
            expected.Ayahs,
            verseCountMismatches,
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
        var sajdaRows = AssembleSajda(source.Sajda, ayahIdsByVerseKey, expected);

        warnings.Add(BuildVerseCountMatchCheck(verseCountMismatches));
        warnings.Add(BuildSajdaDistributionCheck(sajdaRows, expected));

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
        List<string> verseCountMismatches,
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
                verseCountMismatches.Add(
                    $"{divisionLabel} {division.Number}: source={division.VersesCount}, computed={computedCount}");
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

        NavigationValidationChecks.EnsureAllHardChecksPassed(
            NavigationValidationChecks.ValidateRangeCoverage(rows, expectedAyahCount, divisionLabel));

        return rows;
    }

    private static IReadOnlyList<AssembledSajda> AssembleSajda(
        IReadOnlyList<NavigationSajdaDto> sajdas,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        NavigationExpectedCounts expected) =>
        sajdas
            .OrderBy(item => item.SajdahNumber)
            .Select(sajda => new AssembledSajda(
                sajda.SajdahNumber,
                ResolveAyahId(sajda.VerseKey, ayahIdsByVerseKey),
                sajda.VerseKey,
                ParseSajdaType(sajda.SajdahType)))
            .ToList();

    private static NavigationCheckResult BuildVerseCountMatchCheck(IReadOnlyList<string> mismatches) =>
        NavigationValidationChecks.Warning(
            NavigationMetadataInvariants.WarningVerseCountMatch,
            "source verses_count match computed ranges",
            mismatches.Count == 0 ? "all match" : string.Join("; ", mismatches),
            mismatches.Count == 0);

    private static NavigationCheckResult BuildSajdaDistributionCheck(
        IReadOnlyList<AssembledSajda> rows,
        NavigationExpectedCounts expected)
    {
        var required = rows.Count(row => row.SajdahType == SajdahType.Required);
        var optional = rows.Count(row => row.SajdahType == SajdahType.Optional);
        var observed = $"optional={optional}, required={required}";
        var productionExpected =
            $"optional={NavigationMetadataInvariants.ExpectedSajdaOptional}, required={NavigationMetadataInvariants.ExpectedSajdaRequired}";

        if (expected.Sajda != NavigationMetadataInvariants.ExpectedSajda)
        {
            return NavigationValidationChecks.Warning(
                NavigationMetadataInvariants.WarningSajdaDistribution,
                productionExpected,
                $"{observed} (production distribution check not applicable)",
                passed: true);
        }

        var passed = required == NavigationMetadataInvariants.ExpectedSajdaRequired
            && optional == NavigationMetadataInvariants.ExpectedSajdaOptional;

        return NavigationValidationChecks.Warning(
            NavigationMetadataInvariants.WarningSajdaDistribution,
            productionExpected,
            observed,
            passed);
    }

    private static SajdahType ParseSajdaType(string value)
    {
        NavigationValidationChecks.EnsureAllHardChecksPassed([
            NavigationValidationChecks.ValidateSajdaTypeAllowed(value, "source")
        ]);

        return string.Equals(value, "required", StringComparison.Ordinal)
            ? SajdahType.Required
            : SajdahType.Optional;
    }

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

            NavigationValidationChecks.EnsureAllHardChecksPassed([
                NavigationValidationChecks.ValidateHierarchyContainment(child.Number, matches.Count)
            ]);

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

        NavigationValidationChecks.EnsureAllHardChecksPassed([
            NavigationValidationChecks.ValidateAyahColumnsComplete(
                assignments.Count,
                expectedAyahCount,
                expectedAyahCount)
        ]);

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
        NavigationValidationChecks.EnsureAllHardChecksPassed([
            NavigationValidationChecks.ValidateVerseKeyResolves(verseKey, ayahIdsByVerseKey)
        ]);

        return ayahIdsByVerseKey[verseKey];
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
