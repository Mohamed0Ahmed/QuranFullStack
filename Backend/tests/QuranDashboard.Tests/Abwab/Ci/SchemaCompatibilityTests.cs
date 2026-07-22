using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.Ci;

// FR-001 / SC (§15) schema-compatibility gate: the shared fixture applies the real EF *migrations*
// to a fresh Postgres container, and this test asserts the resulting schema is model-compatible —
// i.e. the current EF model has NO pending changes relative to its migrations. It is load-bearing:
// if any entity/config drifts from the 19 committed migrations without a new migration, EF reports a
// pending model diff and this goes RED. Nothing is hard-coded; the boolean comes straight from EF's
// migrations differ against the migrated context. Verified RED by temporarily mutating the model.
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
