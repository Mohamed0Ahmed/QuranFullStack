using System.Reflection;

namespace QuranDashboard.Tests.TestSupport.Execution;

internal static class TestGateCatalog
{
    internal static IReadOnlySet<string> AllowedFeatures { get; } = new HashSet<string>(
        [
            "Abwab",
            "Access",
            "ApiBehavior",
            "Health",
            "Middleware",
            "RateLimiting",
            "FullI3rab",
            "FoundationImport",
            "Linking",
            "PhraseSearch",
            "MushafReader",
            "Mutashabihat",
            "Navigation",
            "QuranTopicsBook",
            "Tafsirs",
            "Translations",
            "Words",
            "WordsDisplay",
            "WordsMorphology",
            "WordsMorphologyEnriched",
            "WordsMorphologyExplorers",
            "WordsRoots",
            "WordsSimpleI3rab",
            "WordsWordTypes",
            "Smoke"
        ],
        StringComparer.Ordinal);

    internal static IReadOnlySet<string> AllowedKinds { get; } = new HashSet<string>(
        ["Fast", "Database", "Migration", "Process", "Canonical"],
        StringComparer.Ordinal);

    internal static IReadOnlySet<string> AllowedGates { get; } = new HashSet<string>(
        ["TierB", "Pipeline", "Smoke"],
        StringComparer.Ordinal);

    internal static IReadOnlySet<string> AllowedConcerns { get; } = new HashSet<string>(
        ["Authorization", "Cli", "Execution", "Schema", "Startup"],
        StringComparer.Ordinal);

    internal static IReadOnlySet<string> AllowedStatePolicies { get; } = new HashSet<string>(
        ["ImmutableSeed", "ResetPerTest", "UniqueKeyIsolation", "FreshLeasePerCase"],
        StringComparer.Ordinal);

    // The one class whose fixture owns an exclusive PostgreSQL server instead of a database leased from the
    // shared runtime. Backend/scripts/test-backend splits any lane that selects it alongside shared-runtime
    // classes into two sequential invocations, so the two server majors never run at once. The name is a
    // constant here and a string in that script; ExclusivePostgreSqlClass_IsADiscoveredTestClass and the
    // script's own catalog check are what stop the two from drifting apart in silence.
    internal const string ExclusivePostgreSqlClass = "QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests";

    internal static IReadOnlyList<string> PipelineClassPrefixes { get; } =
    [
        "QuranDashboard.Tests.Quran.FullI3rab.",
        "QuranDashboard.Tests.Quran.Import.",
        "QuranDashboard.Tests.Quran.Mutashabihat.",
        "QuranDashboard.Tests.Quran.Navigation.",
        "QuranDashboard.Tests.Quran.QuranTopicsBook.",
        "QuranDashboard.Tests.Quran.Tafsirs.",
        "QuranDashboard.Tests.Quran.Translations.",
        "QuranDashboard.Tests.Quran.WordsDisplay.",
        "QuranDashboard.Tests.Quran.WordsMorphology.",
        "QuranDashboard.Tests.Quran.WordsMorphologyEnriched.",
        "QuranDashboard.Tests.Quran.WordsSimpleI3rab."
    ];

    private static readonly Lazy<IReadOnlyList<TestGateEntry>> GateEntryLoader =
        new(LoadGateEntries);

    private static readonly Lazy<IReadOnlyList<TestResourceEntry>> ResourceEntryLoader =
        new(LoadResourceEntries);

    internal static IReadOnlyList<TestGateEntry> GateEntries => GateEntryLoader.Value;

    internal static IReadOnlyList<TestResourceEntry> ResourceEntries => ResourceEntryLoader.Value;

    internal static IReadOnlyList<string> DiscoverTestClasses()
    {
        return DiscoverTestTypes()
            .Select(type => type.FullName!)
            .ToArray();
    }

    internal static IReadOnlyList<DiscoveredCollectedTestClass> DiscoverCollectedTestClasses()
    {
        return DiscoverTestTypes()
            .Select(type => (
                Type: type,
                Collection: type.GetCustomAttribute<CollectionAttribute>(inherit: true)))
            .Where(candidate => candidate.Collection is not null)
            .Select(candidate => new DiscoveredCollectedTestClass(
                candidate.Type.FullName!,
                GetAttributeName(candidate.Type, typeof(CollectionAttribute))))
            .OrderBy(candidate => candidate.ClassName, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<DiscoveredCollectionResource> DiscoverCollectionResources()
    {
        return typeof(TestGateCatalog).Assembly
            .GetTypes()
            .Select(type => (
                Type: type,
                Definition: type.GetCustomAttribute<CollectionDefinitionAttribute>(
                    inherit: false)))
            .Where(candidate => candidate.Definition is not null)
            .Select(candidate =>
            {
                var fixtureTypes = candidate.Type.GetInterfaces()
                    .Where(type => type.IsGenericType
                        && type.GetGenericTypeDefinition() == typeof(ICollectionFixture<>))
                    .Select(type => type.GetGenericArguments().Single())
                    .ToArray();

                if (fixtureTypes.Length != 1)
                {
                    throw new InvalidDataException(
                        $"Collection {candidate.Type.FullName} must declare exactly one collection fixture.");
                }

                return new DiscoveredCollectionResource(
                    GetAttributeName(candidate.Type, typeof(CollectionDefinitionAttribute)),
                    fixtureTypes[0].FullName!,
                    candidate.Definition!.DisableParallelization ? "NonParallel" : "Parallel");
            })
            .OrderBy(resource => resource.CollectionName, StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetAttributeName(MemberInfo member, Type attributeType)
    {
        var attribute = member.CustomAttributes.Single(data => data.AttributeType == attributeType);
        return (string)attribute.ConstructorArguments.Single().Value!;
    }

    internal static IReadOnlyList<string> SelectPrimaryGate(string gate)
    {
        if (!AllowedGates.Contains(gate))
        {
            throw new ArgumentOutOfRangeException(nameof(gate), gate, "Unknown primary gate.");
        }

        return GateEntries
            .Where(entry => entry.Gate == gate)
            .Select(entry => entry.ClassName)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<string> SelectFullBackend()
    {
        return GateEntries
            .Select(entry => entry.ClassName)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<string> SelectKind(string kind)
    {
        if (!AllowedKinds.Contains(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown execution kind.");
        }

        return GateEntries
            .Where(entry => entry.Kind == kind)
            .Select(entry => entry.ClassName)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    internal static PostgreSqlOwnershipShards ShardByPostgreSqlOwnership(IEnumerable<string> classNames)
    {
        var classes = classNames.Order(StringComparer.Ordinal).ToArray();

        return new PostgreSqlOwnershipShards(
            classes.Where(className => className != ExclusivePostgreSqlClass).ToArray(),
            classes.Where(className => className == ExclusivePostgreSqlClass).ToArray());
    }

    private static IReadOnlyList<Type> DiscoverTestTypes()
    {
        return typeof(TestGateCatalog).Assembly
            .GetTypes()
            .Where(type => type.GetMethods(
                    BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly)
                .Any(IsTestMethod))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsTestMethod(MethodInfo method)
    {
        return method.GetCustomAttributes(inherit: true)
            .Any(attribute => attribute is FactAttribute or TheoryAttribute);
    }

    private static IReadOnlyList<TestGateEntry> LoadGateEntries()
    {
        return ReadRows(
                "test-gates.tsv",
                ["FullyQualifiedClassName", "Feature", "Kind", "Gate", "Concerns"],
                requiredColumnCount: 4)
            .Select(columns => new TestGateEntry(
                columns[0],
                columns[1],
                columns[2],
                columns[3],
                columns[4]
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.Ordinal)))
            .ToArray();
    }

    private static IReadOnlyList<TestResourceEntry> LoadResourceEntries()
    {
        return ReadRows(
                "test-resources.tsv",
                ["CollectionName", "ResourceClassName", "ParallelPolicy", "StatePolicy"],
                requiredColumnCount: 4)
            .Select(columns => new TestResourceEntry(
                columns[0],
                columns[1],
                columns[2],
                columns[3]))
            .ToArray();
    }

    private static IReadOnlyList<string[]> ReadRows(
        string fileName,
        IReadOnlyList<string> expectedHeader,
        int requiredColumnCount)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "TestSupport",
            "Execution",
            fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Test catalog was not copied to output: {path}", path);
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"Test catalog is empty: {path}");
        }

        var actualHeader = lines[0].Split('\t');
        if (!actualHeader.SequenceEqual(expectedHeader))
        {
            throw new InvalidDataException(
                $"Unexpected header in {path}: {string.Join(", ", actualHeader)}");
        }

        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select((line, index) =>
            {
                var columns = line.Split('\t');
                if (columns.Length < requiredColumnCount || columns.Length > expectedHeader.Count)
                {
                    throw new InvalidDataException(
                        $"Expected {requiredColumnCount}-{expectedHeader.Count} columns in {path} line {index + 2}, found {columns.Length}.");
                }

                if (columns.Take(requiredColumnCount).Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidDataException(
                        $"Required value is blank in {path} line {index + 2}.");
                }

                Array.Resize(ref columns, expectedHeader.Count);
                columns[^1] ??= string.Empty;
                return columns;
            })
            .ToArray();
    }
}

internal sealed record PostgreSqlOwnershipShards(
    IReadOnlyList<string> SharedRuntime,
    IReadOnlyList<string> ExclusiveServer);

internal sealed record TestGateEntry(
    string ClassName,
    string Feature,
    string Kind,
    string Gate,
    IReadOnlySet<string> Concerns);

internal sealed record TestResourceEntry(
    string CollectionName,
    string ResourceClassName,
    string ParallelPolicy,
    string StatePolicy);

internal sealed record DiscoveredCollectedTestClass(
    string ClassName,
    string CollectionName);

internal sealed record DiscoveredCollectionResource(
    string CollectionName,
    string ResourceClassName,
    string ParallelPolicy);
