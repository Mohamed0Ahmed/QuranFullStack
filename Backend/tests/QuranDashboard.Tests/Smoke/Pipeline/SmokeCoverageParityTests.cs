namespace QuranDashboard.Tests.Smoke.Pipeline;

// The D5 gate: runs everywhere including CI with zero prerequisites — no DB, no host, no resources/.
public sealed class SmokeCoverageParityTests
{
    [Fact]
    public void Every_registered_route_has_a_smoke_catalog_entry()
    {
        var live = SmokeEndpointInventory.ReadNormalizedKeys();
        var missing = live.Except(SmokeRouteCatalog.Cases.Keys).OrderBy(key => key, StringComparer.Ordinal).ToList();

        missing.Should().BeEmpty(
            "every API route needs a SmokeRouteCatalog entry; add entries for: {0}",
            string.Join(", ", missing));
    }

    [Fact]
    public void Every_catalog_entry_maps_to_a_registered_route()
    {
        var live = SmokeEndpointInventory.ReadNormalizedKeys();
        var orphans = SmokeRouteCatalog.Cases.Keys.Except(live).OrderBy(key => key, StringComparer.Ordinal).ToList();

        orphans.Should().BeEmpty(
            "catalog entries must match live routes (stale after a route rename?): {0}",
            string.Join(", ", orphans));
    }
}
