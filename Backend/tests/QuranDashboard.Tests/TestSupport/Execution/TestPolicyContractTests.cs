namespace QuranDashboard.Tests.TestSupport.Execution;

public sealed class TestPolicyContractTests
{
    [Fact]
    public void EveryBackendClass_IsEitherPolicyClassifiedOrExplicitlyUnmigrated()
    {
        var entries = TestGateCatalog.GateEntries;

        entries.Should().OnlyContain(entry =>
            (entry.MigrationState == TestPolicyMigrationState.Migrated) != (entry.Policy == null));
        entries.Select(entry => entry.ClassName)
            .Should()
            .BeEquivalentTo(TestGateCatalog.DiscoverTestClasses());

        var resolvePolicies = () => entries
            .Where(entry => entry.MigrationState == TestPolicyMigrationState.Migrated)
            .Select(TestGateCatalog.ResolveEffectivePolicy)
            .ToArray();
        resolvePolicies.Should().NotThrow();

        var compiledCollections = TestGateCatalog.DiscoverCollectedTestClasses()
            .ToDictionary(entry => entry.ClassName, entry => entry.CollectionName, StringComparer.Ordinal);
        foreach (var entry in entries.Where(candidate =>
                     candidate.MigrationState == TestPolicyMigrationState.Migrated))
        {
            if (compiledCollections.TryGetValue(entry.ClassName, out var collectionName))
            {
                entry.ResourceCollection.Should().Be(collectionName);
            }
            else
            {
                entry.ResourceCollection.Should().BeNull();
            }
        }
    }

    [Fact]
    public void EveryFixtureResource_IsEitherPolicyClassifiedOrExplicitlyUnmigrated()
    {
        TestGateCatalog.ResourceEntries.Should().OnlyContain(entry =>
            (entry.MigrationState == TestPolicyMigrationState.Migrated) != (entry.Policy == null));
    }

    [Fact]
    public void EffectivePolicy_UsesTheStrictestClassAndFixtureBehavior()
    {
        var classPolicy = new BackendTestPolicyMetadata(
            BackendTestPolicy.CanonicalReader,
            new HashSet<TestDataClass> { TestDataClass.CanonicalQuranData },
            new HashSet<TestDataClass>(),
            TestDatabaseTarget.TestDatabase,
            DestructiveRehearsalSubtype.None);
        var resourcePolicy = new TestResourcePolicyMetadata(
            new HashSet<TestDataClass> { TestDataClass.MutableApplicationState },
            TestResetBehavior.MutableApplicationState,
            TestDatabaseTarget.TestDatabase,
            new HashSet<TestApiStartupEffect> { TestApiStartupEffect.MutableApi });

        var effective = TestPolicyContract.Combine(classPolicy, resourcePolicy);

        effective.Policy.Should().Be(BackendTestPolicy.MutableWriter);
        effective.Reads.Should().BeEquivalentTo([TestDataClass.CanonicalQuranData]);
        effective.Writes.Should().BeEquivalentTo([TestDataClass.MutableApplicationState]);
        effective.Target.Should().Be(TestDatabaseTarget.TestDatabase);
    }

    [Fact]
    public void ProtectedStateWrites_RequireAnApprovedDestructiveRehearsalTargetAndSubtype()
    {
        var invalid = new BackendTestPolicyMetadata(
            BackendTestPolicy.MutableWriter,
            new HashSet<TestDataClass>(),
            new HashSet<TestDataClass> { TestDataClass.SchemaState },
            TestDatabaseTarget.TestDatabase,
            DestructiveRehearsalSubtype.None);

        var validate = () => TestPolicyContract.Validate(invalid, "contract case");

        validate.Should().Throw<InvalidDataException>()
            .WithMessage("*Protected State*DestructiveRehearsal*approved subtype*Rehearsal Database*");
    }

    [Fact]
    public void PolicyContractTests_AreAClassifiedFastNoDbTracer()
    {
        var entry = TestGateCatalog.GateEntries.Single(candidate =>
            candidate.ClassName == typeof(TestPolicyContractTests).FullName);

        entry.MigrationState.Should().Be(TestPolicyMigrationState.Migrated);
        entry.Policy.Should().NotBeNull();
        entry.Policy!.Policy.Should().Be(BackendTestPolicy.FastNoDb);
        entry.Policy.Reads.Should().BeEmpty();
        entry.Policy.Writes.Should().BeEmpty();
        entry.Policy.Target.Should().Be(TestDatabaseTarget.None);
        entry.ResourceCollection.Should().BeNull();
    }
}
