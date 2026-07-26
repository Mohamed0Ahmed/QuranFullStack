using QuranDashboard.Tests.Abwab.Ci;

namespace QuranDashboard.Tests.Smoke.Pipeline;

internal static class SmokeEndpointInventory
{
    // Same enumeration ContractDriftTests gates CI with: bare ServiceCollection + ApiExplorer,
    // no DB, no host boot — keys look like "GET /api/abwab/templates/{doorTemplateId}".
    public static HashSet<string> ReadNormalizedKeys() => ApiContractSources.ReadLiveEndpoints();
}
