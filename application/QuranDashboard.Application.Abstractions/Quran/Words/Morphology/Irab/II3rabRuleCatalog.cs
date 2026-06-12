namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabRuleCatalog
{
    IReadOnlyList<I3rabRuleSeedRow> Rows();

    bool TryGet(string signatureKey, out I3rabRuleSeedRow row);
}
