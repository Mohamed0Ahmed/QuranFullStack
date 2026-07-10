using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

/// <summary>
/// CP-0 smoke test: confirms the Feature 016 shared fixture can start PostgreSQL
/// (or connect to the real DB), build the production DI provider, ensure the
/// schema, and load the committed source-safe seed slice. This guards that later
/// story-phase tests can resolve handlers/readers from the fixture without
/// infrastructure setup failures.
/// </summary>
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class MorphologyExplorersFixtureSmokeTests(MorphologyExplorersTestFixture fixture)
{
    [Fact]
    public async Task Fixture_StartsAndSeedsDatabase_Successfully()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.Database.CanConnectAsync()).Should().BeTrue();
    }
}
