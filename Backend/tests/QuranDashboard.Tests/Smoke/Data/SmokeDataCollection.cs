namespace QuranDashboard.Tests.Smoke.Data;

// The shared lock keeps this multi-route snapshot stable while preserving framework-level reader
// parallelism. The fixture never provisions or restores database state.
[CollectionDefinition(nameof(SmokeDataCollection))]
public sealed class SmokeDataCollection : ICollectionFixture<SmokeDataFixture>;
