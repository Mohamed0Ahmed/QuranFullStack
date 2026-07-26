using System.Diagnostics;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Abwab.Templates;

// T075 (US2 half) — the §15.3 numeric budgets for the two template hot paths: applying a template to
// one real category, and the separate template-history read. Hardware/data assumptions and the
// measured values are recorded in
// Backend/report/feature-030-abwab-relationships-templates/performance-budgets.md.
//
// As in RelationshipBudgetTests, the QUERY-COUNT assertions are the load-bearing ones: they are
// deterministic and hardware-independent, so they are what actually catches an N+1 regression. The
// wall-clock p95 assertions carry a deliberately wide margin and exist only to catch an
// order-of-magnitude collapse.
[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateBudgetTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const int SmallTemplateRootCount = 2;
    private const int LargeTemplateRootCount = 10;
    private const int TemplateDepth = 1;

    private const int MeasurementIterations = 10;

    private const int SmallTemplateNodeCount = SmallTemplateRootCount * (TemplateDepth + 1);
    private const int LargeTemplateNodeCount = LargeTemplateRootCount * (TemplateDepth + 1);

    // MEASURED (see performance-budgets.md): applying a 4-node template cost 30 queries and a 20-node
    // template 86, so each created category costs a CONSTANT 3.5 queries and the rest is fixed
    // per-application overhead. Budget = 4: query counts are deterministic and hardware-independent,
    // and any genuine N+1 regression — re-resolving protection or the name guard per created row —
    // adds at least one whole query per node.
    private const double MeasuredPerNodeQueryCost = 3.5;
    private const double ApplicationPerNodeQueryBudget = 4;

    // MEASURED: the history read is a CONSTANT 2 queries (the capped page, then the generation) no
    // matter how long the history is. Budget = measured baseline + 1 margin.
    private const int MeasuredHistoryQueryCount = 2;
    private const int HistoryQueryBudget = MeasuredHistoryQueryCount + 1;

    // MEASURED p95 on the reference machine: application 61 ms, history read 6 ms. Frozen as
    // max(measured × 5, 250 ms), the shape 029/US1 used — the multiplier governs when the measurement
    // is large, the floor when it is small. CI hardware is unknown, so these catch an
    // order-of-magnitude collapse, never normal variance.
    private const int P95FloorMs = 250;
    private const int ApplicationP95BudgetMs = 305;
    private const int HistoryP95BudgetMs = P95FloorMs;

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TemplateApplication_CostsAConstantNumberOfQueriesPerCreatedCategory()
    {
        var small = await CountQueriesForApplicationAsync(SmallTemplateRootCount);
        var large = await CountQueriesForApplicationAsync(LargeTemplateRootCount);

        var perNode = (large - small) / (double)(LargeTemplateNodeCount - SmallTemplateNodeCount);

        perNode.Should().BeLessThanOrEqualTo(
            ApplicationPerNodeQueryBudget,
            $"each created category must cost a constant {MeasuredPerNodeQueryCost} queries, so a "
            + "template five times the size costs five times the rows and not five times the round-trips");
    }

    [Fact]
    public async Task TheTemplateHistoryRead_CostsAConstantQueryCount_AndIsCappedInSize()
    {
        var doorTemplateId = await SeedHistoryAsync(IAbwabTemplateReadPort.MaxHistoryEntries + 5);

        var (queryCount, history) = await CountQueriesForHistoryAsync(doorTemplateId);

        queryCount.Should().BeLessThanOrEqualTo(
            HistoryQueryBudget,
            $"the history read is a constant {MeasuredHistoryQueryCount} queries recorded in performance-budgets.md");
        history.Entries.Should().HaveCount(
            IAbwabTemplateReadPort.MaxHistoryEntries,
            "the projection is capped, so an old template cannot return an unbounded payload");
        history.HasMore.Should().BeTrue("truncation is reported, never silent");
    }

    [Fact]
    public async Task TemplateApplication_StaysWithinTheMeasuredP95Budget()
    {
        // Seeded up front and consumed one target per iteration: seeding inside the timed region would
        // charge fixture construction to the budget, and re-applying to the SAME target would collide
        // on the destination name rather than measure the application path.
        var (template, targets) = await SeedApplicationFixtureAsync(SmallTemplateRootCount, MeasurementIterations + 1);
        var remaining = new Queue<Guid>(targets);

        var p95 = await MeasureP95Async(async () =>
        {
            await using var db = CreateWriteContext(interceptor: null);
            var writePort = AbwabWriterTestHarness.CreateTemplateWritePort(db);
            await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template, remaining.Dequeue());
        });

        p95.Should().BeLessThanOrEqualTo(
            ApplicationP95BudgetMs,
            "template application p95 must stay within the frozen budget recorded in performance-budgets.md");
    }

    [Fact]
    public async Task TheTemplateHistoryRead_StaysWithinTheMeasuredP95Budget()
    {
        var doorTemplateId = await SeedHistoryAsync(IAbwabTemplateReadPort.MaxHistoryEntries + 5);

        var p95 = await MeasureP95Async(async () =>
        {
            await using var db = SecurityTestHarness.CreateContext(fixture);
            await AbwabWriterTestHarness.CreateTemplateReadPort(db).GetHistoryAsync(doorTemplateId, CancellationToken.None);
        });
        p95.Should().BeLessThanOrEqualTo(
            HistoryP95BudgetMs,
            "the template-history read p95 must stay within the frozen budget recorded in performance-budgets.md");
    }

    private static async Task<int> MeasureP95Async(Func<Task> operation)
    {
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

    private async Task<int> CountQueriesForApplicationAsync(int rootCount)
    {
        var (template, targets) = await SeedApplicationFixtureAsync(rootCount, targetCount: 1);

        var interceptor = new SqlCommandCountInterceptor();
        await using var db = CreateWriteContext(interceptor);
        var writePort = AbwabWriterTestHarness.CreateTemplateWritePort(db);

        // Reset after wiring so connection opening and fixture reads are not charged to the budget.
        interceptor.Reset();
        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template, targets[0]);

        return interceptor.CommandCount;
    }

    // Every target is a distinct root: a root category name is globally unique, so reusing one label
    // across the fixture would fail on the name guard instead of measuring anything.
    private async Task<(Guid Template, IReadOnlyList<Guid> Targets)> SeedApplicationFixtureAsync(int rootCount, int targetCount)
    {
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount, TemplateDepth);

        var targets = new List<Guid>(targetCount);
        for (var index = 0; index < targetCount; index++)
        {
            var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(
                fixture, childCount: 0, label: $"هدف {Guid.NewGuid():N}");
            targets.Add(target.CategoryId);
        }

        return (template.DoorTemplateId, targets);
    }

    private async Task<(int QueryCount, TemplateHistoryDto History)> CountQueriesForHistoryAsync(Guid doorTemplateId)
    {
        var interceptor = new SqlCommandCountInterceptor();
        await using var db = CreateWriteContext(interceptor);

        interceptor.Reset();
        var history = await AbwabWriterTestHarness.CreateTemplateReadPort(db).GetHistoryAsync(doorTemplateId, CancellationToken.None);

        return (interceptor.CommandCount, history);
    }

    // Real audited edits, not hand-inserted rows: the history projection matches on the stored payload,
    // so a fabricated event would measure a predicate the production writer never produces.
    private async Task<Guid> SeedHistoryAsync(int editCount)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;

        var doorTemplateId = await writePort.AddDoorTemplateAsync(
            new AddDoorTemplateCommand("قالب السجل", null, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        for (var edit = 0; edit < editCount; edit++)
        {
            var version = (await db.AbwabDoorTemplates.AsNoTracking().SingleAsync(t => t.DoorTemplateId == doorTemplateId)).Version;
            await writePort.EditDoorTemplateAsync(
                new EditDoorTemplateCommand(doorTemplateId, $"قالب السجل {edit}", null, version, ExpectedTimelineGeneration.Of(0), "tester"),
                CancellationToken.None);
        }

        return doorTemplateId;
    }

    private QuranDashboardDbContext CreateWriteContext(SqlCommandCountInterceptor? interceptor)
    {
        var builder = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));

        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new QuranDashboardDbContext(builder.Options);
    }
}
