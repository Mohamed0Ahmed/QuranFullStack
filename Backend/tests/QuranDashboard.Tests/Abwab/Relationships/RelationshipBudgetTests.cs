using System.Diagnostics;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Abwab.Relationships;

// T075 (US1 half) — the §15.3 numeric budgets for the two relationship hot paths, frozen from
// MEASURED numbers. Hardware/data assumptions and the measured values are recorded in
// Backend/report/feature-030-abwab-relationships-templates/performance-budgets.md.
//
// The load-bearing assertions here are the QUERY-COUNT budgets, exactly as 029's DeepTreeBudgetTests
// framed the same obligation: a query count is deterministic and hardware-independent, so it is what
// actually catches an N+1 regression. The wall-clock p95 assertions carry a deliberately wide margin
// over the measured baseline — they exist to catch an order-of-magnitude collapse, and must never be
// read as a precise timing contract on unknown CI hardware.
[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipBudgetTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const int ChainDepth = 40;
    private const int WideFanOut = 40;
    private const int AffectedCategoryScale = 200;

    private const int MeasurementIterations = 20;

    // MEASURED (see performance-budgets.md): cycle validation issues one batched query per BFS
    // LEVEL — never one per edge. A depth-40 chain measured 40 queries; a depth-1 star with 40 edges
    // measured 2. Budget = one per level + margin.
    private const int CycleQueryBudgetPerLevel = 1;
    private const int MeasuredWideGraphQueryCount = 2;
    private const int WideGraphCycleQueryBudget = MeasuredWideGraphQueryCount + 1;

    // MEASURED: GetDormantCountsAsync is a CONSTANT 2 queries (attached rows, then their endpoints)
    // — identical at 2 and at 200 affected categories. Budget = measured baseline (2) + 1 margin.
    private const int MeasuredDormancyBaselineQueryCount = 2;
    private const int DormancyQueryBudget = MeasuredDormancyBaselineQueryCount + 1;

    // MEASURED p95 on the reference machine: cycle validation 2.82 ms, dormancy counts 6.72 ms.
    // Frozen as max(measured * 5, 250 ms) following the shape 029 used for its browser budget; at
    // single-digit-millisecond measurements the floor dominates, because CI hardware is unknown and
    // this assertion exists to catch an order-of-magnitude collapse, not to police normal variance.
    private const int P95FloorMs = 250;
    private const int CycleP95BudgetMs = P95FloorMs;
    private const int DormancyP95BudgetMs = P95FloorMs;

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DirectionalCycleValidation_CostsOneQueryPerLevel_NeverOnePerEdge()
    {
        var chain = await SeedDirectionalChainAsync(ChainDepth);
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;

        // Closing the chain back on itself forces the BFS to walk every level before rejecting.
        var queryCount = await CountQueriesForCycleValidationAsync(chain[^1].CategoryId, chain[0].CategoryId, expectRejection: true);

        queryCount.Should().BeLessThanOrEqualTo(
            (ChainDepth * CycleQueryBudgetPerLevel) + 2,
            $"cycle validation must batch one query per BFS level over a depth-{ChainDepth} chain, never one per edge");

        writePort.Should().NotBeNull();
    }

    [Fact]
    public async Task DirectionalCycleValidation_OnAWideGraph_IsProportionalToDepthNotEdgeCount()
    {
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, WideFanOut + 2);
        var broader = endpoints[0];
        var narrower = endpoints[1];

        // A star: `narrower` points at WideFanOut leaves. Depth is 1 no matter how wide it gets.
        var edges = Enumerable.Range(2, WideFanOut)
            .Select(i => AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(narrower.CategoryId, endpoints[i].CategoryId))
            .ToArray();
        await AbwabTreeSeeding.InsertAsync(fixture, edges);

        var queryCount = await CountQueriesForCycleValidationAsync(broader.CategoryId, narrower.CategoryId, expectRejection: false);

        queryCount.Should().BeLessThanOrEqualTo(
            WideGraphCycleQueryBudget,
            $"a depth-1 star with {WideFanOut} edges must cost the same as a depth-1 star with 1 edge (no N+1 over edges)");
    }

    [Fact]
    public async Task DormantCountsProjection_CostsAConstantQueryCount_RegardlessOfAffectedSetSize()
    {
        var (small, large) = await SeedDormancyFixtureAsync();

        var smallCount = await CountQueriesForDormantCountsAsync(small);
        var largeCount = await CountQueriesForDormantCountsAsync(large);

        largeCount.Should().BeLessThanOrEqualTo(
            DormancyQueryBudget,
            $"the dormancy projection over {AffectedCategoryScale} affected categories must stay within the measured baseline " +
            $"({MeasuredDormancyBaselineQueryCount}) plus margin, recorded in performance-budgets.md");
        largeCount.Should().Be(smallCount, "the projection must batch endpoints, never resolve them one row at a time");
    }

    [Fact]
    public async Task DirectionalCycleValidation_StaysWithinTheMeasuredP95Budget()
    {
        var chain = await SeedDirectionalChainAsync(ChainDepth);

        var p95 = await MeasureP95Async(async () =>
        {
            await using var context = CreateContext(interceptor: null);
            var store = new EfCategoryRelationshipStore(context);
            await store.GetActiveDirectionalTargetsAsync([chain[0].CategoryId], null, CancellationToken.None);
        });

        p95.Should().BeLessThanOrEqualTo(
            CycleP95BudgetMs,
            "directional cycle validation p95 must stay within the frozen budget recorded in performance-budgets.md");
    }

    [Fact]
    public async Task DormantCountsProjection_StaysWithinTheMeasuredP95Budget()
    {
        var (_, large) = await SeedDormancyFixtureAsync();

        var p95 = await MeasureP95Async(async () =>
        {
            await using var context = CreateContext(interceptor: null);
            var readPort = new EfAbwabRelationshipReadPort(context, new FixedServerClock(DateTimeOffset.UnixEpoch));
            await readPort.GetDormantCountsAsync(large, CancellationToken.None);
        });

        p95.Should().BeLessThanOrEqualTo(
            DormancyP95BudgetMs,
            "the subtree-dormancy projection p95 must stay within the frozen budget recorded in performance-budgets.md");
    }

    private static async Task<int> MeasureP95Async(Func<Task> operation)
    {
        // One untimed warm-up so connection-pool and query-plan setup is not charged to the budget.
        await operation();

        var samples = new List<double>(MeasurementIterations);
        for (var iteration = 0; iteration < MeasurementIterations; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        var index = (int)Math.Ceiling(samples.Count * 0.95) - 1;
        return (int)Math.Ceiling(samples[Math.Clamp(index, 0, samples.Count - 1)]);
    }

    private async Task<int> CountQueriesForCycleValidationAsync(Guid broaderCategoryId, Guid narrowerCategoryId, bool expectRejection)
    {
        var interceptor = new SqlCommandCountInterceptor();
        await using var context = CreateContext(interceptor);
        var store = new EfCategoryRelationshipStore(context);

        var visited = new HashSet<Guid> { narrowerCategoryId };
        var frontier = new List<Guid> { narrowerCategoryId };
        var rejected = false;

        while (frontier.Count > 0)
        {
            var reached = await store.GetActiveDirectionalTargetsAsync(frontier, null, CancellationToken.None);
            if (reached.Contains(broaderCategoryId))
            {
                rejected = true;
                break;
            }

            frontier = [.. reached.Where(visited.Add)];
        }

        rejected.Should().Be(expectRejection, "the budget fixture must exercise the branch it claims to measure");
        return interceptor.CommandCount;
    }

    private async Task<int> CountQueriesForDormantCountsAsync(IReadOnlyCollection<Guid> affectedCategoryIds)
    {
        var interceptor = new SqlCommandCountInterceptor();
        await using var context = CreateContext(interceptor);
        var readPort = new EfAbwabRelationshipReadPort(context, new FixedServerClock(DateTimeOffset.UnixEpoch));

        await readPort.GetDormantCountsAsync(affectedCategoryIds, CancellationToken.None);
        return interceptor.CommandCount;
    }

    private QuranDashboardDbContext CreateContext(SqlCommandCountInterceptor? interceptor)
    {
        var builder = new DbContextOptionsBuilder<QuranDashboardDbContext>().UseNpgsql(fixture.ConnectionString);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new QuranDashboardDbContext(builder.Options);
    }

    private async Task<IReadOnlyList<Category>> SeedDirectionalChainAsync(int depth)
    {
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, depth + 1, "حلقة");
        var edges = Enumerable.Range(0, depth)
            .Select(i => AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(endpoints[i].CategoryId, endpoints[i + 1].CategoryId))
            .ToArray();

        await AbwabTreeSeeding.InsertAsync(fixture, edges);
        return endpoints;
    }

    // Half the pairs are made genuinely dormant so the grouping branch is actually measured, not
    // just the empty-result path.
    private async Task SoftDeleteEndpointsAsync(IReadOnlyCollection<Guid> categoryIds)
    {
        await using var context = CreateContext(interceptor: null);
        var rows = await context.Set<Category>().Where(c => categoryIds.Contains(c.CategoryId)).ToListAsync();
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        }

        await context.SaveChangesAsync();
    }

    private async Task<(IReadOnlyList<Guid> Small, IReadOnlyList<Guid> Large)> SeedDormancyFixtureAsync()
    {
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, AffectedCategoryScale, "متأثر");
        var edges = Enumerable.Range(0, AffectedCategoryScale / 2)
            .Select(i =>
            {
                var (lower, higher) = CategoryRelationship.Canonicalize(
                    endpoints[i * 2].CategoryId, endpoints[(i * 2) + 1].CategoryId);
                return AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower, higher, RelationshipType.Similar);
            })
            .ToArray();

        await AbwabTreeSeeding.InsertAsync(fixture, edges);
        await SoftDeleteEndpointsAsync(endpoints.Where((_, i) => i % 4 == 0).Select(c => c.CategoryId).ToList());

        return (
            endpoints.Take(2).Select(c => c.CategoryId).ToList(),
            endpoints.Select(c => c.CategoryId).ToList());
    }
}
