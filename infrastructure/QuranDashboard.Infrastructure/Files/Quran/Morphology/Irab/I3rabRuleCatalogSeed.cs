using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

public sealed class I3rabRuleCatalogSeed : II3rabRuleCatalog
{
    private readonly IReadOnlyDictionary<string, I3rabRuleSeedRow> rowsBySignature;

    public I3rabRuleCatalogSeed()
    {
        var rows = I3rabRuleCatalogSeedData.BuildRows();
        rowsBySignature = rows.ToDictionary(row => row.SignatureKey, StringComparer.Ordinal);
    }

    public IReadOnlyList<I3rabRuleSeedRow> Rows() => rowsBySignature.Values.OrderBy(row => row.SortOrder).ToList();

    public bool TryGet(string signatureKey, out I3rabRuleSeedRow row) =>
        rowsBySignature.TryGetValue(signatureKey, out row!);
}
