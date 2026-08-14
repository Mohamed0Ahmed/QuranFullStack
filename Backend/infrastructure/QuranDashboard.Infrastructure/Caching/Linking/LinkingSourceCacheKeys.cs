using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public static class LinkingSourceCacheKeys
{
    private const string Prefix = "linking:source:v2";

    public static string For(LinkingSourceKind kind, string sourceIdentity, long linkingDataRevision)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentity);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}:{linkingDataRevision}:{LinkingSourceTokens.ToToken(kind)}:{WordTypesCacheKeys.HashParts(sourceIdentity)}");
    }

    public static string AyahText(int ayahId, long linkingDataRevision) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"linking:ayah-text:v2:{linkingDataRevision}:{ayahId}");
}
