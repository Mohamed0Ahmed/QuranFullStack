using QuranDashboard.Tests.Smoke._Fixtures;

namespace QuranDashboard.Tests.Smoke;

[CollectionDefinition(nameof(SmokeCollection))]
public sealed class SmokeCollection : ICollectionFixture<SmokeApiFixture>;
