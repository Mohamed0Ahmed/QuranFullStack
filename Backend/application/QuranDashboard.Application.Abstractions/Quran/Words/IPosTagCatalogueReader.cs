namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// Read-only existence lookup over the POS-tag catalogue (<c>quran_pos_tags</c>). Used to
/// validate the Unique Words <c>primaryType</c> association filter (Feature 026, US7)
/// against the catalogue, so an unknown POS code is rejected with a controlled 400 rather
/// than silently matching nothing. No new endpoint — the frontend feeds its type select
/// from the existing word-types tree read; this reader only guards direct-API input.
/// </summary>
public interface IPosTagCatalogueReader
{
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken);
}
