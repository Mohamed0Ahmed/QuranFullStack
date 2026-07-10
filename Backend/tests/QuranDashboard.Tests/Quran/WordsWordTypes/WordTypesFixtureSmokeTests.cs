using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesFixtureSmokeTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task Fixture_StartsAndSeedsDatabase_Successfully()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.Database.CanConnectAsync()).Should().BeTrue();
    }
}
