namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public interface II3rabRuleCatalog
{
    IReadOnlyList<I3rabRuleSeedRow> Rows();

    bool TryGet(string signatureKey, out I3rabRuleSeedRow row);
}
