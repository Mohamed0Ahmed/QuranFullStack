using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.Caching.Linking;

public static class LinkingSourceCacheKeys
{
    private const string Prefix = "linking:source:v1";

    public static string For(LinkingSourceKind kind, string sourceIdentity)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentity);

        return $"{Prefix}:{LinkingSourceTokens.ToToken(kind)}:{WordTypesCacheKeys.HashParts(sourceIdentity)}";
    }

    public static string AyahText(int ayahId) =>
        string.Create(CultureInfo.InvariantCulture, $"linking:ayah-text:v1:{ayahId}");
}
