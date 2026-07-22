using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.Ci;

[Collection(nameof(AbwabDbCollection))]
public sealed class SchemaCompatibilityTests
{
    private readonly PostgresFixture _fixture;

    public SchemaCompatibilityTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void MigratedSchema_HasNoPendingModelChanges()
    {
        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.Database.HasPendingModelChanges().Should().BeFalse(
            "the applied migrations must yield a schema whose model matches the current EF model; a "
            + "pending model diff means the model drifted from its migrations (a migration is missing).");
    }
}
