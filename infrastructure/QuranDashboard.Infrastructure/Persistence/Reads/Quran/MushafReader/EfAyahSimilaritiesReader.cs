using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfAyahSimilaritiesReader(QuranDashboardDbContext db) : IAyahSimilaritiesReader
{
    public async Task<SimilarAyahsResponse?> GetSimilarAyahsAsync(string verseKey, CancellationToken ct)
    {
        var selectedAyah = await db.QuranAyahs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.VerseKey == verseKey, ct);

        if (selectedAyah is null)
        {
            return null;
        }

        var outgoingLinks = await db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.SourceAyahId == selectedAyah.Id)
            .Select(link => new SimilarLinkRow(
                link.TargetAyahId,
                link.Score,
                link.Coverage,
                link.MatchedWordsCount,
                IsOutgoing: true))
            .ToListAsync(ct);

        var incomingLinks = await db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.TargetAyahId == selectedAyah.Id)
            .Select(link => new SimilarLinkRow(
                link.SourceAyahId,
                link.Score,
                link.Coverage,
                link.MatchedWordsCount,
                IsOutgoing: false))
            .ToListAsync(ct);

        var mergedLinks = MergeSimilarLinks(outgoingLinks, incomingLinks);
        if (mergedLinks.Count == 0)
        {
            return new SimilarAyahsResponse(verseKey, 0, []);
        }

        var relatedAyahIds = mergedLinks.Keys.ToList();
        var relatedAyahs = await db.QuranAyahs
            .AsNoTracking()
            .Include(a => a.Surah)
            .Where(a => relatedAyahIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var items = mergedLinks.Values
            .Select(merged =>
            {
                var ayah = relatedAyahs[merged.RelatedAyahId];
                return MapSimilarAyahItem(ayah, merged);
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.SurahNumber)
            .ThenBy(item => item.AyahNumber)
            .ToList();

        return new SimilarAyahsResponse(verseKey, items.Count, items);
    }

    private static Dictionary<int, MergedSimilarLink> MergeSimilarLinks(
        IReadOnlyList<SimilarLinkRow> outgoingLinks,
        IReadOnlyList<SimilarLinkRow> incomingLinks)
    {
        var merged = new Dictionary<int, MergedSimilarLink>();

        foreach (var link in outgoingLinks)
        {
            merged[link.RelatedAyahId] = new MergedSimilarLink(
                link.RelatedAyahId,
                Outgoing: link,
                Incoming: null);
        }

        foreach (var link in incomingLinks)
        {
            if (merged.TryGetValue(link.RelatedAyahId, out var existing))
            {
                merged[link.RelatedAyahId] = existing with { Incoming = link };
                continue;
            }

            merged[link.RelatedAyahId] = new MergedSimilarLink(
                link.RelatedAyahId,
                Outgoing: null,
                Incoming: link);
        }

        return merged;
    }

    private static SimilarAyahItemDto MapSimilarAyahItem(Ayah ayah, MergedSimilarLink merged)
    {
        var primaryLink = SelectPrimaryLink(merged);
        var hasBothDirections = merged.Outgoing is not null && merged.Incoming is not null;
        var direction = hasBothDirections
            ? SimilarAyahRelationshipDirections.Bidirectional
            : merged.Outgoing is not null
                ? SimilarAyahRelationshipDirections.Outgoing
                : SimilarAyahRelationshipDirections.Incoming;

        return new SimilarAyahItemDto(
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.Surah.NameArabic,
            ayah.AyahNumber,
            ayah.PageFrom,
            ayah.JuzNumber ?? 0,
            ayah.HizbNumber ?? 0,
            ayah.RubNumber ?? 0,
            ayah.TextUthmani,
            primaryLink.Score,
            primaryLink.Coverage,
            primaryLink.MatchedWordsCount,
            direction,
            hasBothDirections);
    }

    private static SimilarLinkRow SelectPrimaryLink(MergedSimilarLink merged)
    {
        if (merged.Outgoing is null)
        {
            return merged.Incoming!;
        }

        if (merged.Incoming is null)
        {
            return merged.Outgoing;
        }

        return merged.Outgoing.Score >= merged.Incoming.Score
            ? merged.Outgoing
            : merged.Incoming;
    }

    private sealed record SimilarLinkRow(
        int RelatedAyahId,
        short Score,
        short Coverage,
        short MatchedWordsCount,
        bool IsOutgoing);

    private sealed record MergedSimilarLink(
        int RelatedAyahId,
        SimilarLinkRow? Outgoing,
        SimilarLinkRow? Incoming);
}
