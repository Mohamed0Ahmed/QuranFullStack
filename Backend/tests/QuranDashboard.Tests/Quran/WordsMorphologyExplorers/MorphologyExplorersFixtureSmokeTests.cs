namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

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
