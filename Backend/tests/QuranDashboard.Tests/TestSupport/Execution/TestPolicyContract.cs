namespace QuranDashboard.Tests.TestSupport.Execution;

internal enum BackendTestPolicy
{
    FastNoDb,
    CanonicalReader,
    GuardedReader,
    MutableWriter,
    DestructiveRehearsal,
}

internal enum TestDataClass
{
    CanonicalQuranData,
    SystemCatalogue,
    MutableApplicationState,
    SchemaState,
}

internal enum TestDatabaseTarget
{
    None,
    TestDatabase,
    EmptyScratch,
    FullRehearsal,
}

internal enum DestructiveRehearsalSubtype
{
    None,
    CanonicalImport,
    CanonicalRebuild,
    CanonicalGeneration,
    Migration,
    SystemCatalogueReconciliation,
    SchemaDrift,
    PhraseSearchIndexBuild,
    Recovery,
}

internal enum TestResetBehavior
{
    None,
    MutableApplicationState,
}

internal enum TestApiStartupEffect
{
    None,
    ReadOnlyApi,
    MutableApi,
    DestructiveApi,
}

internal enum TestPolicyMigrationState
{
    Migrated,
    Unmigrated,
}

internal sealed record BackendTestPolicyMetadata(
    BackendTestPolicy Policy,
    IReadOnlySet<TestDataClass> Reads,
    IReadOnlySet<TestDataClass> Writes,
    TestDatabaseTarget Target,
    DestructiveRehearsalSubtype DestructiveSubtype);

internal sealed record TestResourcePolicyMetadata(
    IReadOnlySet<TestDataClass> SetupWrites,
    TestResetBehavior ResetBehavior,
    TestDatabaseTarget Target,
    IReadOnlySet<TestApiStartupEffect> StartupEffects);

internal sealed record EffectiveTestPolicy(
    BackendTestPolicy Policy,
    IReadOnlySet<TestDataClass> Reads,
    IReadOnlySet<TestDataClass> Writes,
    TestDatabaseTarget Target,
    DestructiveRehearsalSubtype DestructiveSubtype);

internal static class TestPolicyContract
{
    private static readonly IReadOnlySet<TestDataClass> ProtectedState = new HashSet<TestDataClass>
    {
        TestDataClass.CanonicalQuranData,
        TestDataClass.SystemCatalogue,
        TestDataClass.SchemaState,
    };

    internal static EffectiveTestPolicy Combine(
        BackendTestPolicyMetadata classPolicy,
        TestResourcePolicyMetadata? resourcePolicy)
    {
        Validate(classPolicy, "class policy");
        if (resourcePolicy is null)
        {
            return new EffectiveTestPolicy(
                classPolicy.Policy,
                classPolicy.Reads,
                classPolicy.Writes,
                classPolicy.Target,
                classPolicy.DestructiveSubtype);
        }

        Validate(resourcePolicy, "fixture/resource policy");
        var effectiveTarget = CombineTargets(classPolicy.Target, resourcePolicy.Target);
        var effectivePolicy = Strictest(
            classPolicy.Policy,
            MinimumPolicyFor(resourcePolicy));
        var effectiveWrites = classPolicy.Writes
            .Concat(resourcePolicy.SetupWrites)
            .ToHashSet();
        var effective = new BackendTestPolicyMetadata(
            effectivePolicy,
            classPolicy.Reads,
            effectiveWrites,
            effectiveTarget,
            classPolicy.DestructiveSubtype);

        Validate(effective, "effective class and fixture/resource policy");
        return new EffectiveTestPolicy(
            effective.Policy,
            effective.Reads,
            effective.Writes,
            effective.Target,
            effective.DestructiveSubtype);
    }

    internal static void Validate(BackendTestPolicyMetadata metadata, string subject)
    {
        var writesProtectedState = metadata.Writes.Any(ProtectedState.Contains);
        var writesOnlyMutableState = metadata.Writes.All(
            dataClass => dataClass == TestDataClass.MutableApplicationState);

        if (writesProtectedState
            && (metadata.Policy != BackendTestPolicy.DestructiveRehearsal
                || metadata.DestructiveSubtype == DestructiveRehearsalSubtype.None
                || metadata.Target is not (
                    TestDatabaseTarget.EmptyScratch or TestDatabaseTarget.FullRehearsal)))
        {
            throw new InvalidDataException(
                $"{subject} writes Protected State and requires DestructiveRehearsal, an approved subtype, and an empty-scratch or full Rehearsal Database target.");
        }

        switch (metadata.Policy)
        {
            case BackendTestPolicy.FastNoDb when metadata.Reads.Count != 0
                || metadata.Writes.Count != 0
                || metadata.Target != TestDatabaseTarget.None
                || metadata.DestructiveSubtype != DestructiveRehearsalSubtype.None:
                throw Invalid(subject, "FastNoDb cannot declare database reads, writes, a target, or a destructive subtype.");
            case BackendTestPolicy.CanonicalReader or BackendTestPolicy.GuardedReader
                when metadata.Writes.Count != 0
                    || metadata.Target != TestDatabaseTarget.TestDatabase
                    || metadata.DestructiveSubtype != DestructiveRehearsalSubtype.None:
                throw Invalid(subject, "reader policies require the Test Database, no writes, and no destructive subtype.");
            case BackendTestPolicy.MutableWriter
                when !writesOnlyMutableState
                    || metadata.Target != TestDatabaseTarget.TestDatabase
                    || metadata.DestructiveSubtype != DestructiveRehearsalSubtype.None:
                throw Invalid(subject, "MutableWriter may write only Mutable Application State on the Test Database.");
            case BackendTestPolicy.DestructiveRehearsal
                when metadata.DestructiveSubtype == DestructiveRehearsalSubtype.None
                    || metadata.Target is not (
                        TestDatabaseTarget.EmptyScratch or TestDatabaseTarget.FullRehearsal):
                throw Invalid(subject, "DestructiveRehearsal requires an approved subtype and a Rehearsal Database target.");
            case not BackendTestPolicy.FastNoDb when metadata.Target == TestDatabaseTarget.None:
                throw Invalid(subject, "database-aware policies require an explicit database target.");
        }
    }

    internal static void Validate(TestResourcePolicyMetadata metadata, string subject)
    {
        if (metadata.StartupEffects.Contains(TestApiStartupEffect.None))
        {
            throw Invalid(subject, "None cannot be combined with explicit API startup effects.");
        }

        var protectedWrites = metadata.SetupWrites.Any(ProtectedState.Contains);
        if (protectedWrites
            && metadata.Target is not (
                TestDatabaseTarget.EmptyScratch or TestDatabaseTarget.FullRehearsal))
        {
            throw Invalid(subject, "Protected State setup writes require a Rehearsal Database target.");
        }

        if ((metadata.SetupWrites.Count != 0
                || metadata.ResetBehavior != TestResetBehavior.None
                || metadata.StartupEffects.Any(effect => effect != TestApiStartupEffect.None))
            && metadata.Target == TestDatabaseTarget.None)
        {
            throw Invalid(subject, "fixture/resource effects require an explicit database target.");
        }

        if (metadata.Target == TestDatabaseTarget.TestDatabase
            && metadata.StartupEffects.Contains(TestApiStartupEffect.DestructiveApi))
        {
            throw Invalid(subject, "a destructive API cannot target the persistent Test Database.");
        }
    }

    private static BackendTestPolicy MinimumPolicyFor(TestResourcePolicyMetadata metadata)
    {
        if (metadata.SetupWrites.Any(ProtectedState.Contains)
            || metadata.StartupEffects.Contains(TestApiStartupEffect.DestructiveApi)
            || metadata.Target is TestDatabaseTarget.EmptyScratch or TestDatabaseTarget.FullRehearsal)
        {
            return BackendTestPolicy.DestructiveRehearsal;
        }

        if (metadata.SetupWrites.Count != 0
            || metadata.ResetBehavior == TestResetBehavior.MutableApplicationState
            || metadata.StartupEffects.Contains(TestApiStartupEffect.MutableApi))
        {
            return BackendTestPolicy.MutableWriter;
        }

        return metadata.Target == TestDatabaseTarget.None
            ? BackendTestPolicy.FastNoDb
            : BackendTestPolicy.CanonicalReader;
    }

    private static BackendTestPolicy Strictest(
        BackendTestPolicy left,
        BackendTestPolicy right) =>
        (BackendTestPolicy)Math.Max((int)left, (int)right);

    private static TestDatabaseTarget CombineTargets(
        TestDatabaseTarget classTarget,
        TestDatabaseTarget resourceTarget)
    {
        if (classTarget == TestDatabaseTarget.None)
        {
            return resourceTarget;
        }
        if (resourceTarget == TestDatabaseTarget.None || resourceTarget == classTarget)
        {
            return classTarget;
        }

        throw new InvalidDataException(
            $"Class and fixture/resource metadata declare contradictory database targets: {classTarget} and {resourceTarget}.");
    }

    private static InvalidDataException Invalid(string subject, string message) =>
        new($"Invalid {subject}: {message}");
}

internal static class TestPolicyMetadataParser
{
    internal static (TestPolicyMigrationState MigrationState, BackendTestPolicyMetadata? Policy)
        ParseClassPolicy(IReadOnlyList<string> columns, string path, int lineNumber)
    {
        var state = ParseEnum<TestPolicyMigrationState>(columns[11], path, lineNumber, "MigrationState");
        var values = columns.Skip(5).Take(6).ToArray();
        if (state == TestPolicyMigrationState.Unmigrated)
        {
            RequireAllBlank(values, path, lineNumber, "unmigrated class policy");
            return (state, null);
        }

        var metadata = new BackendTestPolicyMetadata(
            ParseEnum<BackendTestPolicy>(columns[5], path, lineNumber, "BackendPolicy"),
            ParseSet<TestDataClass>(columns[6], path, lineNumber, "DataReads"),
            ParseSet<TestDataClass>(columns[7], path, lineNumber, "DataWrites"),
            ParseEnum<TestDatabaseTarget>(columns[8], path, lineNumber, "DatabaseTarget"),
            ParseEnum<DestructiveRehearsalSubtype>(columns[9], path, lineNumber, "DestructiveSubtype"));
        if (string.IsNullOrWhiteSpace(columns[10]))
        {
            throw Invalid(path, lineNumber, "ResourceCollection must be explicit; use None when no fixture/resource applies");
        }
        TestPolicyContract.Validate(metadata, $"class policy in {path} line {lineNumber}");
        return (state, metadata);
    }

    internal static (TestPolicyMigrationState MigrationState, TestResourcePolicyMetadata? Policy)
        ParseResourcePolicy(IReadOnlyList<string> columns, string path, int lineNumber)
    {
        var state = ParseEnum<TestPolicyMigrationState>(columns[8], path, lineNumber, "MigrationState");
        var values = columns.Skip(4).Take(4).ToArray();
        if (state == TestPolicyMigrationState.Unmigrated)
        {
            RequireAllBlank(values, path, lineNumber, "unmigrated fixture/resource policy");
            return (state, null);
        }

        var metadata = new TestResourcePolicyMetadata(
            ParseSet<TestDataClass>(columns[4], path, lineNumber, "SetupWrites"),
            ParseEnum<TestResetBehavior>(columns[5], path, lineNumber, "ResetBehavior"),
            ParseEnum<TestDatabaseTarget>(columns[6], path, lineNumber, "DatabaseTarget"),
            ParseSet<TestApiStartupEffect>(columns[7], path, lineNumber, "StartupEffects"));
        TestPolicyContract.Validate(metadata, $"fixture/resource policy in {path} line {lineNumber}");
        return (state, metadata);
    }

    private static IReadOnlySet<T> ParseSet<T>(
        string value,
        string path,
        int lineNumber,
        string column)
        where T : struct, Enum
    {
        if (value == "None")
        {
            return new HashSet<T>();
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(path, lineNumber, $"{column} must be explicit; use None for an empty set");
        }

        var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => ParseEnum<T>(item, path, lineNumber, column))
            .ToArray();
        if (values.Length != values.Distinct().Count())
        {
            throw Invalid(path, lineNumber, $"{column} contains duplicate values");
        }
        return values.ToHashSet();
    }

    private static T ParseEnum<T>(string value, string path, int lineNumber, string column)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw Invalid(path, lineNumber, $"{column} has unsupported value '{value}'");
        }
        return parsed;
    }

    private static void RequireAllBlank(
        IEnumerable<string> values,
        string path,
        int lineNumber,
        string subject)
    {
        if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            throw Invalid(path, lineNumber, $"{subject} must leave policy fields blank");
        }
    }

    private static InvalidDataException Invalid(string path, int lineNumber, string message) =>
        new($"Invalid test policy metadata in {path} line {lineNumber}: {message}.");
}
