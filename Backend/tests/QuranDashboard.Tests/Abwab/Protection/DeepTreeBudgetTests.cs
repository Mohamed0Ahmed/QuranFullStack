using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class DeepTreeBudgetTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const int RepresentativeDepth = 200;

    // Measured baseline against a representative RepresentativeDepth=200 real-PostgreSQL chain
    // (see Backend/application/QuranDashboard.Application/Abwab/Protection/README.md): resolution
    // reads the denormalized AncestorIds column instead of walking parent links, so its query count
    // is constant, never proportional to depth. Budget = measured baseline (3) + a safety margin.
    private const int MeasuredBaselineQueryCount = 3;
    private const int QueryBudget = MeasuredBaselineQueryCount + 2;

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Resolution_OnADeepTree_StaysWithinTheMeasuredQueryBudget_AndIsNotProportionalToDepth()
    {
        var shallowLeafId = await SeedChainAsync(depth: 5);
        var deepLeafId = await SeedChainAsync(depth: RepresentativeDepth);

        var shallowCount = await CountQueriesForProfileResolutionAsync(shallowLeafId);
        var deepCount = await CountQueriesForProfileResolutionAsync(deepLeafId);

        deepCount.Should().BeLessThanOrEqualTo(
            QueryBudget,
            $"protection resolution at depth {RepresentativeDepth} must stay within the measured baseline " +
            $"({MeasuredBaselineQueryCount}) plus margin, recorded in the protection resolver README");
        deepCount.Should().Be(shallowCount, "resolution must read current AncestorIds directly, never walk parent links one row at a time (no N+1)");
    }

    private async Task<int> CountQueriesForProfileResolutionAsync(Guid categoryId)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var builder = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor);

        await using var context = new QuranDashboardDbContext(builder.Options);
        var resolver = new ProtectionResolver(new EfManualProtectionReadPort(context), new FixedServerClock(DateTimeOffset.UnixEpoch));

        var profile = await resolver.ResolveProfileAsync(categoryId, CancellationToken.None);
        profile.Should().NotBeNull();

        return interceptor.CommandCount;
    }

    private async Task<Guid> SeedChainAsync(int depth)
    {
        var section = AbwabTreeSeeding.NewSection($"قسم اختبار عمق {depth}");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory($"باب اختبار عمق {depth}", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var current = root;
        for (var level = 0; level < depth; level++)
        {
            var child = AbwabTreeSeeding.NewChildCategory($"مستوى {depth}-{level}", current, siblingOrder: 0);
            await AbwabTreeSeeding.InsertAsync(fixture, child);
            current = child;
        }

        return current.CategoryId;
    }
}
