namespace QuranDashboard.Tests.TestSupport.Execution;

public sealed class TestGateCatalogTests
{
    [Fact]
    public void EveryDiscoveredTestClass_HasExactlyOneCatalogEntry()
    {
        var discoveredClasses = TestGateCatalog.DiscoverTestClasses();
        var catalogGroups = TestGateCatalog.GateEntries
            .GroupBy(entry => entry.ClassName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var missingClasses = discoveredClasses
            .Where(className => !catalogGroups.ContainsKey(className))
            .ToArray();
        var duplicateClasses = catalogGroups
            .Where(group => group.Value != 1)
            .Select(group => $"{group.Key} ({group.Value})")
            .ToArray();

        missingClasses.Should().BeEmpty();
        duplicateClasses.Should().BeEmpty();
    }

    [Fact]
    public void CatalogEntries_NameDiscoveredTestClasses()
    {
        var discoveredClasses = TestGateCatalog.DiscoverTestClasses();

        TestGateCatalog.GateEntries
            .Select(entry => entry.ClassName)
            .Should()
            .OnlyContain(className => discoveredClasses.Contains(className));
    }

    [Fact]
    public void AccessTestClasses_MapToAccessFeature()
    {
        var accessEntries = TestGateCatalog.GateEntries
            .Where(entry =>
                entry.ClassName.StartsWith("QuranDashboard.Tests.Api.Access.", StringComparison.Ordinal)
                || entry.ClassName.StartsWith("QuranDashboard.Tests.TestSupport.Access.", StringComparison.Ordinal))
            .ToArray();

        accessEntries.Should().NotBeEmpty();
        accessEntries.Should().OnlyContain(entry => entry.Feature == "Access");
    }

    [Fact]
    public void CatalogAxes_UseOnlyAllowedValues()
    {
        TestGateCatalog.GateEntries.Should().NotBeEmpty();
        TestGateCatalog.GateEntries.Should().OnlyContain(entry =>
            TestGateCatalog.AllowedFeatures.Contains(entry.Feature)
            && TestGateCatalog.AllowedKinds.Contains(entry.Kind)
            && TestGateCatalog.AllowedGates.Contains(entry.Gate)
            && entry.Concerns.All(TestGateCatalog.AllowedConcerns.Contains));
    }

    [Fact]
    public void CollectionResources_MatchCompiledCollectionDefinitions()
    {
        var discoveredResources = TestGateCatalog.DiscoverCollectionResources();

        TestGateCatalog.ResourceEntries
            .Select(entry => entry.CollectionName)
            .Should()
            .BeEquivalentTo(discoveredResources.Select(resource => resource.CollectionName));

        foreach (var discoveredResource in discoveredResources)
        {
            var catalogResource = TestGateCatalog.ResourceEntries
                .Single(entry => entry.CollectionName == discoveredResource.CollectionName);

            catalogResource.ResourceClassName.Should().Be(discoveredResource.ResourceClassName);
            catalogResource.ParallelPolicy.Should().Be(discoveredResource.ParallelPolicy);
            TestGateCatalog.AllowedStatePolicies.Should().Contain(catalogResource.StatePolicy);
        }
    }

    [Fact]
    public void EveryCollectedTestClass_UsesACatalogedResource()
    {
        var catalogedCollections = TestGateCatalog.ResourceEntries
            .Select(entry => entry.CollectionName)
            .ToHashSet(StringComparer.Ordinal);

        var missingCollections = TestGateCatalog.DiscoverCollectedTestClasses()
            .Where(testClass => !catalogedCollections.Contains(testClass.CollectionName))
            .Select(testClass => $"{testClass.ClassName}: {testClass.CollectionName}")
            .ToArray();

        missingCollections.Should().BeEmpty();
    }

    [Fact]
    public void FastTestClasses_DoNotUseResourceCollections()
    {
        var collectedClasses = TestGateCatalog.DiscoverCollectedTestClasses()
            .Select(testClass => testClass.ClassName)
            .ToHashSet(StringComparer.Ordinal);

        TestGateCatalog.GateEntries
            .Where(entry => entry.Kind == "Fast")
            .Select(entry => entry.ClassName)
            .Should()
            .NotIntersectWith(collectedClasses);
    }

    [Fact]
    public void PipelineNamespaces_MapToPipelineGate()
    {
        var pipelineEntries = TestGateCatalog.GateEntries
            .Where(entry => TestGateCatalog.PipelineClassPrefixes.Any(
                prefix => entry.ClassName.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        pipelineEntries.Should().NotBeEmpty();
        pipelineEntries.Should().OnlyContain(entry => entry.Gate == "Pipeline" || entry.Gate == "TierB");
        TestGateCatalog.GateEntries
            .Where(entry => entry.Gate == "Pipeline")
            .Should()
            .OnlyContain(entry => TestGateCatalog.PipelineClassPrefixes.Any(
                prefix => entry.ClassName.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void SmokeClasses_MapToSmokeGateWithOnlyDataSmokeCanonical()
    {
        const string smokeDataClass = "QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests";
        var smokeEntries = TestGateCatalog.GateEntries
            .Where(entry => entry.ClassName.StartsWith(
                "QuranDashboard.Tests.Smoke.",
                StringComparison.Ordinal))
            .ToArray();

        smokeEntries.Should().NotBeEmpty();
        smokeEntries.Should().OnlyContain(entry => entry.Gate == "Smoke");
        smokeEntries.Single(entry => entry.ClassName == smokeDataClass)
            .Kind.Should().Be("Canonical");
        smokeEntries.Where(entry => entry.ClassName != smokeDataClass)
            .Should().OnlyContain(entry => entry.Kind != "Canonical");
    }

    [Fact]
    public void PrimaryGates_PartitionEveryDiscoveredTestClass()
    {
        var tierB = TestGateCatalog.SelectPrimaryGate("TierB").ToHashSet(StringComparer.Ordinal);
        var pipeline = TestGateCatalog.SelectPrimaryGate("Pipeline").ToHashSet(StringComparer.Ordinal);
        var smoke = TestGateCatalog.SelectPrimaryGate("Smoke").ToHashSet(StringComparer.Ordinal);
        var release = TestGateCatalog.SelectPrimaryGate("Release").ToHashSet(StringComparer.Ordinal);

        tierB.Should().NotIntersectWith(pipeline);
        tierB.Should().NotIntersectWith(smoke);
        tierB.Should().NotIntersectWith(release);
        pipeline.Should().NotIntersectWith(smoke);
        pipeline.Should().NotIntersectWith(release);
        smoke.Should().NotIntersectWith(release);
        tierB.Concat(pipeline).Concat(smoke).Concat(release)
            .Should()
            .BeEquivalentTo(TestGateCatalog.DiscoverTestClasses());
    }

    [Fact]
    public void FullBackendSelection_UsesEveryDiscoveredTestClass()
    {
        TestGateCatalog.SelectFullBackend()
            .Should()
            .BeEquivalentTo(TestGateCatalog.DiscoverTestClasses());
    }

    // The shards below select by class name, but what starts the exclusive postgres:18-alpine server is
    // membership of SmokeDataCollection. A second class joining that collection would construct the
    // exclusive fixture inside the shared-runtime shard; this names that mistake here instead of leaving it
    // to a confusing "two containers must never run at once" throw at runtime.
    [Fact]
    public void ExclusivePostgreSqlClass_IsTheOnlyCanonicalClassOwningItsServer()
    {
        TestGateCatalog.DiscoverTestClasses()
            .Should()
            .Contain(TestGateCatalog.ExclusivePostgreSqlClass);

        TestGateCatalog.SelectKind("Canonical")
            .Should()
            .Contain(TestGateCatalog.ExclusivePostgreSqlClass);

        TestGateCatalog.DiscoverCollectedTestClasses()
            .Where(testClass => testClass.CollectionName == nameof(Smoke.Data.SmokeDataCollection))
            .Select(testClass => testClass.ClassName)
            .Should()
            .Equal(TestGateCatalog.ExclusivePostgreSqlClass);
    }

    // The two invocations Backend/scripts/test-backend runs for a lane that would otherwise start
    // postgres:16-alpine and postgres:18-alpine in one process. Losing a class between them would silently
    // drop coverage; sharing one would put both servers back in the same process.
    [Theory]
    [InlineData("canonical-data")]
    [InlineData("pre-pr")]
    public void PostgreSqlOwnershipShards_PartitionTheLaneTheyReplace(string lane)
    {
        var laneClasses = lane == "canonical-data"
            ? TestGateCatalog.SelectKind("Canonical")
            : TestGateCatalog.SelectFullBackend()
                .Where(className => TestGateCatalog.GateEntries.Single(entry => entry.ClassName == className).Kind != "Release");

        var shards = TestGateCatalog.ShardByPostgreSqlOwnership(laneClasses);

        shards.SharedRuntime.Should().NotIntersectWith(shards.ExclusiveServer);
        shards.SharedRuntime.Concat(shards.ExclusiveServer).Should().BeEquivalentTo(laneClasses);
        shards.ExclusiveServer.Should().Equal(TestGateCatalog.ExclusivePostgreSqlClass);
        shards.SharedRuntime.Should().NotBeEmpty();
    }
}
