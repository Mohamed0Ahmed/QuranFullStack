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
