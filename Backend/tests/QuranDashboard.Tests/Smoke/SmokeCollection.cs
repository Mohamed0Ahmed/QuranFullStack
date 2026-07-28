namespace QuranDashboard.Tests.Smoke;

// Its own collection, never shared with AccessCollection or a pipeline collection: later smoke phases
// mutate the users table and the shared role cache, which would otherwise leak across families.
[CollectionDefinition(nameof(SmokeCollection))]
public sealed class SmokeCollection : ICollectionFixture<SmokeApiFixture>;
