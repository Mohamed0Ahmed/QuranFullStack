namespace QuranDashboard.Tests.Smoke.Data;

// Separate from SmokeCollection, not merely a separate fixture: SmokeRoutePipelineTests derives all 48 of
// its expectations from a migrated-but-empty schema, and an xUnit collection is what guarantees the
// seeded container is never the one those tests reach.
//
// Nonparallel because its fixture owns the whole PostgreSQL server for the process rather than one leased
// database: no other collection may be running while the exclusive postgres:18-alpine container is up.
[CollectionDefinition(nameof(SmokeDataCollection), DisableParallelization = true)]
public sealed class SmokeDataCollection : ICollectionFixture<SmokeDataFixture>;
