using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public sealed class LinkingResolvedSourceCompact
{
    private LinkingResolvedSourceCompact(
        string sourceIdentity,
        IReadOnlyList<int> ayahIds,
        IReadOnlyList<CompactAyah> ayahs,
        bool includesAyahMarkers)
    {
        SourceIdentity = sourceIdentity;
        AyahIds = ayahIds;
        Ayahs = ayahs;
        AyahsById = ayahs.ToDictionary(ayah => ayah.AyahId);
        IncludesAyahMarkers = includesAyahMarkers;
    }

    public string SourceIdentity { get; }

    public IReadOnlyList<int> AyahIds { get; }

    public IReadOnlyList<CompactAyah> Ayahs { get; }

    public IReadOnlyDictionary<int, CompactAyah> AyahsById { get; }

    public bool IncludesAyahMarkers { get; }

    public int AyahCount => Ayahs.Count;

    public long ReferenceWeight => Ayahs.Sum(ayah =>
        3L + ayah.QuranWordIds.Count + ayah.MatchedQuranWordIds.Count);

    public static LinkingResolvedSourceCompact From(
        LinkingResolvedSourceDto resolved,
        bool includesAyahMarkers)
    {
        ArgumentNullException.ThrowIfNull(resolved);

        var ayahIds = new int[resolved.Ayahs.Count];
        var ayahs = new CompactAyah[resolved.Ayahs.Count];

        for (var index = 0; index < resolved.Ayahs.Count; index++)
        {
            var ayah = resolved.Ayahs[index];
            var wordIds = new int[ayah.Words.Count];

            for (var wordIndex = 0; wordIndex < ayah.Words.Count; wordIndex++)
            {
                wordIds[wordIndex] = ayah.Words[wordIndex].QuranWordId;
            }

            ayahIds[index] = ayah.AyahId;
            ayahs[index] = new CompactAyah(ayah.AyahId, wordIds, [.. ayah.MatchedQuranWordIds]);
        }

        return new LinkingResolvedSourceCompact(
            resolved.SourceIdentity,
            ayahIds,
            ayahs,
            includesAyahMarkers);
    }

    public static LinkingResolvedSourceCompact Create(
        string sourceIdentity,
        IReadOnlyList<CompactAyah> ayahs,
        bool includesAyahMarkers) =>
        new(sourceIdentity, [.. ayahs.Select(ayah => ayah.AyahId)], ayahs, includesAyahMarkers);

    public sealed record CompactAyah(
        int AyahId,
        IReadOnlyList<int> QuranWordIds,
        IReadOnlyList<int> MatchedQuranWordIds);
}
