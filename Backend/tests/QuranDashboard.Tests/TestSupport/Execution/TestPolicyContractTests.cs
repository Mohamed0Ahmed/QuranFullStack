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

    [Fact]
    public void AccessMutableWriters_UseTheSinglePersistentMutableDatabaseResource()
    {
        string[] expectedClasses =
        [
            "QuranDashboard.Tests.Api.Access.AccessAdministrationEndpointTests",
            "QuranDashboard.Tests.Api.Access.AccessAuditEventPersistenceTests",
            "QuranDashboard.Tests.Api.Access.AccessCollectionResetContractTests",
            "QuranDashboard.Tests.Api.Access.AccessMeEndpointTests",
            "QuranDashboard.Tests.Api.Access.AccessRolesTests",
            "QuranDashboard.Tests.Api.Access.AuthorizationPipelineTests",
            "QuranDashboard.Tests.Api.Access.AuthorizationRejectionResponseTests",
            "QuranDashboard.Tests.Api.Access.AuthorizationRequirementHandlerTests",
            "QuranDashboard.Tests.Api.Access.AuthorizationStateResolverTests",
            "QuranDashboard.Tests.Api.Access.DeviceSessionLifecycleTests",
            "QuranDashboard.Tests.Api.Access.EmailIdentityPreflightTests",
            "QuranDashboard.Tests.Api.Access.LogtoSubjectRelinkEndpointTests",
            "QuranDashboard.Tests.Api.Access.OwnerReconciliationServiceTests",
            "QuranDashboard.Tests.Api.Access.UserProvisioningServiceTests",
        ];

        var entries = TestGateCatalog.GateEntries
            .Where(entry => expectedClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();
        var expectedReads = new HashSet<TestDataClass>
        {
            TestDataClass.SystemCatalogue,
            TestDataClass.MutableApplicationState,
        };
        var expectedWrites = new HashSet<TestDataClass> { TestDataClass.MutableApplicationState };

        entries.Select(entry => entry.ClassName).Should().BeEquivalentTo(expectedClasses);
        entries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.MutableWriter
            && entry.Policy.Reads.IsSupersetOf(expectedReads)
            && entry.Policy.Writes.SetEquals(expectedWrites)
            && entry.Policy.Target == TestDatabaseTarget.TestDatabase
            && entry.Policy.DestructiveSubtype == DestructiveRehearsalSubtype.None
            && entry.ResourceCollection == "MutableDatabaseCollection");
        entries.Single(entry => entry.ClassName.EndsWith(
                ".AccessCollectionResetContractTests",
                StringComparison.Ordinal))
            .Policy!.Reads.Should().Contain(TestDataClass.SchemaState);

        var resource = TestGateCatalog.ResourceEntries.Single(entry =>
            entry.CollectionName == "MutableDatabaseCollection");
        resource.ParallelPolicy.Should().Be("NonParallel");
        resource.StatePolicy.Should().Be("ResetPerTest");
        resource.MigrationState.Should().Be(TestPolicyMigrationState.Migrated);
        resource.Policy.Should().NotBeNull();
        resource.Policy!.SetupWrites.Should().BeEmpty();
        resource.Policy.ResetBehavior.Should().Be(TestResetBehavior.MutableApplicationState);
        resource.Policy.Target.Should().Be(TestDatabaseTarget.TestDatabase);
        resource.Policy.StartupEffects.Should().BeEquivalentTo([TestApiStartupEffect.MutableApi]);
    }

    [Fact]
    public void AbwabMutableWriters_UseTheSinglePersistentMutableDatabaseResource()
    {
        string[] expectedClasses =
        [
            "QuranDashboard.Tests.Abwab.AbwabCollectionResetContractTests",
            "QuranDashboard.Tests.Abwab.AbwabDoorWriteBehaviorTests",
            "QuranDashboard.Tests.Abwab.AbwabRelationWriteBehaviorTests",
            "QuranDashboard.Tests.Abwab.AbwabSchemaTests",
            "QuranDashboard.Tests.Abwab.AbwabTemplateApplyBehaviorTests",
            "QuranDashboard.Tests.Api.Abwab.AbwabInclusionProjectionTests",
        ];

        var entries = TestGateCatalog.GateEntries
            .Where(entry => expectedClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();
        var expectedReads = new HashSet<TestDataClass>
        {
            TestDataClass.SystemCatalogue,
            TestDataClass.MutableApplicationState,
        };
        var expectedWrites = new HashSet<TestDataClass> { TestDataClass.MutableApplicationState };

        entries.Select(entry => entry.ClassName).Should().BeEquivalentTo(expectedClasses);
        entries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.MutableWriter
            && entry.Policy.Reads.IsSupersetOf(expectedReads)
            && entry.Policy.Writes.SetEquals(expectedWrites)
            && entry.Policy.Target == TestDatabaseTarget.TestDatabase
            && entry.Policy.DestructiveSubtype == DestructiveRehearsalSubtype.None
            && entry.ResourceCollection == "MutableDatabaseCollection");
        entries.Single(entry => entry.ClassName.EndsWith(
                ".AbwabCollectionResetContractTests",
                StringComparison.Ordinal))
            .Policy!.Reads.Should().Contain(TestDataClass.SchemaState);
        entries.Single(entry => entry.ClassName.EndsWith(
                ".AbwabSchemaTests",
                StringComparison.Ordinal))
            .Policy!.Reads.Should().Contain(TestDataClass.SchemaState);
        entries.Single(entry => entry.ClassName.EndsWith(
                ".AbwabInclusionProjectionTests",
                StringComparison.Ordinal))
            .Policy!.Reads.Should().Contain(TestDataClass.CanonicalQuranData);
    }

    [Fact]
    public void MorphologyDisplayAndI3rabPipelines_UseBoundedScratchRehearsals()
    {
        string[] scratchCollections =
        [
            "MorphologyImportTestCollection",
            "WordsDisplayTestCollection",
            "DisplayWordsRealImportCollection",
            "I3rabGenerationTestCollection",
            "FullI3rabImportTestCollection",
            "FullI3rabSchemaTestCollection",
        ];
        string[] fastClasses =
        [
            "QuranDashboard.Tests.Quran.WordsMorphology.MorphologyAssemblerTests",
            "QuranDashboard.Tests.Quran.WordsMorphology.WordLemmaNormalizationApplierTests",
            "QuranDashboard.Tests.Quran.WordsMorphologyEnriched.EnrichedMorphologyArtifactTests",
            "QuranDashboard.Tests.Quran.WordsMorphologyEnriched.EnrichedMorphologyDryValidatorTests",
            "QuranDashboard.Tests.Quran.WordsMorphologyEnriched.EnrichedMorphologyImportSourceTests",
            "QuranDashboard.Tests.Quran.WordsMorphologyEnriched.EnrichedMorphologyManifestReaderTests",
            "QuranDashboard.Tests.Quran.FullI3rab.FullI3rabManifestReaderTests",
        ];

        var scratchResources = TestGateCatalog.ResourceEntries
            .Where(entry => scratchCollections.Contains(entry.CollectionName, StringComparer.Ordinal))
            .ToArray();
        var scratchClasses = TestGateCatalog.GateEntries
            .Where(entry => entry.ResourceCollection is not null
                && scratchCollections.Contains(entry.ResourceCollection, StringComparer.Ordinal))
            .ToArray();
        var fastEntries = TestGateCatalog.GateEntries
            .Where(entry => fastClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();

        scratchResources.Select(entry => entry.CollectionName).Should().BeEquivalentTo(scratchCollections);
        scratchResources.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Target == TestDatabaseTarget.EmptyScratch
            && entry.Policy.ResetBehavior == TestResetBehavior.None
            && entry.Policy.StartupEffects.Count == 0
            && entry.Policy.SetupWrites.Contains(TestDataClass.SchemaState));
        scratchClasses.Should().NotBeEmpty();
        scratchClasses.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.DestructiveRehearsal
            && entry.Policy.Target == TestDatabaseTarget.EmptyScratch
            && new[]
            {
                DestructiveRehearsalSubtype.CanonicalImport,
                DestructiveRehearsalSubtype.CanonicalRebuild,
                DestructiveRehearsalSubtype.CanonicalGeneration,
            }.Contains(entry.Policy.DestructiveSubtype));
        fastEntries.Select(entry => entry.ClassName).Should().BeEquivalentTo(fastClasses);
        fastEntries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.FastNoDb
            && entry.Policy.Reads.Count == 0
            && entry.Policy.Writes.Count == 0
            && entry.Policy.Target == TestDatabaseTarget.None
            && entry.ResourceCollection == null);
    }

    [Fact]
    public void LinkingMutableWriters_UseTheSinglePersistentMutableDatabaseResource()
    {
        string[] expectedClasses =
        [
            "QuranDashboard.Tests.Api.Linking.LinkingCollectionResetContractTests",
            "QuranDashboard.Tests.Api.Linking.LinkingConfirmationIdempotencyTests",
            "QuranDashboard.Tests.Api.Linking.LinkingRecoveryAndAtomicityTests",
            "QuranDashboard.Tests.Api.Linking.LinkingSuccessfulJourneyTests",
        ];

        var entries = TestGateCatalog.GateEntries
            .Where(entry => expectedClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();

        entries.Select(entry => entry.ClassName).Should().BeEquivalentTo(expectedClasses);
        entries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.MutableWriter
            && entry.Policy.Reads.IsSupersetOf(
                new HashSet<TestDataClass>
                {
                    TestDataClass.CanonicalQuranData,
                    TestDataClass.SystemCatalogue,
                    TestDataClass.MutableApplicationState,
                })
            && entry.Policy.Writes.SetEquals(
                new HashSet<TestDataClass> { TestDataClass.MutableApplicationState })
            && entry.Policy.Target == TestDatabaseTarget.TestDatabase
            && entry.Policy.DestructiveSubtype == DestructiveRehearsalSubtype.None
            && entry.ResourceCollection == "MutableDatabaseCollection");
        entries.Single(entry => entry.ClassName.EndsWith(
                ".LinkingCollectionResetContractTests",
                StringComparison.Ordinal))
            .Policy!.Reads.Should().Contain(TestDataClass.SchemaState);
    }

    [Fact]
    public void SmokePolicies_SeparateMutableApiBehaviorFromReadOnlyAndFastContracts()
    {
        string[] mutableClasses =
        [
            "QuranDashboard.Tests.Smoke.SmokeAccessAdministrationAuthorizationTests",
            "QuranDashboard.Tests.Smoke.SmokeAbwabWriteAuthorizationTests",
            "QuranDashboard.Tests.Smoke.SmokeAuthPipelineTests",
            "QuranDashboard.Tests.Smoke.SmokeMutableBootGuardTests",
        ];
        string[] readerClasses =
        [
            "QuranDashboard.Tests.Smoke.SmokeAuthPipelineReadTests",
            "QuranDashboard.Tests.Smoke.SmokeCoverageParityTests",
            "QuranDashboard.Tests.Smoke.SmokeReadOnlyBootGuardTests",
            "QuranDashboard.Tests.Smoke.SmokeRoutePipelineTests",
        ];

        var mutableEntries = TestGateCatalog.GateEntries
            .Where(entry => mutableClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();
        mutableEntries.Select(entry => entry.ClassName).Should().BeEquivalentTo(mutableClasses);
        mutableEntries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.MutableWriter
            && entry.Policy.Writes.All(dataClass => dataClass == TestDataClass.MutableApplicationState)
            && entry.Policy.Target == TestDatabaseTarget.TestDatabase
            && entry.ResourceCollection == "MutableDatabaseCollection");

        var readerEntries = TestGateCatalog.GateEntries
            .Where(entry => readerClasses.Contains(entry.ClassName, StringComparer.Ordinal))
            .ToArray();
        readerEntries.Select(entry => entry.ClassName).Should().BeEquivalentTo(readerClasses);
        readerEntries.Should().OnlyContain(entry =>
            entry.MigrationState == TestPolicyMigrationState.Migrated
            && entry.Policy != null
            && entry.Policy.Policy == BackendTestPolicy.GuardedReader
            && entry.Policy.Writes.Count == 0
            && entry.Policy.Target == TestDatabaseTarget.TestDatabase
            && entry.ResourceCollection == "SmokeDataCollection");

        var baseline = TestGateCatalog.GateEntries.Single(entry =>
            entry.ClassName == "QuranDashboard.Tests.Smoke.SmokeRouteBaselineTests");
        baseline.MigrationState.Should().Be(TestPolicyMigrationState.Migrated);
        baseline.Policy!.Policy.Should().Be(BackendTestPolicy.FastNoDb);
        baseline.ResourceCollection.Should().BeNull();
    }
}
