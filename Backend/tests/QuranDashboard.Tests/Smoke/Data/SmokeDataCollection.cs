namespace QuranDashboard.Tests.Smoke.Data;

// Separate from SmokeCollection, not merely a separate fixture: SmokeRoutePipelineTests derives all 48 of
// its expectations from a migrated-but-empty schema, and an xUnit collection is what guarantees the
// seeded container is never the one those tests reach.
[CollectionDefinition(nameof(SmokeDataCollection))]
public sealed class SmokeDataCollection : ICollectionFixture<SmokeDataFixture>;
