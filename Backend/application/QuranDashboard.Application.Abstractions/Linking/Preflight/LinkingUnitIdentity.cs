using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public static class LinkingUnitIdentity
{
    public static string For(
        bool isGrouped,
        IReadOnlyList<LinkingOperationAyahIntent> ayahs)
    {
        ArgumentNullException.ThrowIfNull(ayahs);

        var identity = new StringBuilder(isGrouped ? "grouped" : "independent");
        foreach (var ayah in ayahs
                     .OrderBy(ayah => ayah.SurahNumber)
                     .ThenBy(ayah => ayah.AyahNumber)
                     .ThenBy(ayah => ayah.AyahId))
        {
            identity.Append('|');
            identity.Append(ayah.AyahId.ToString(CultureInfo.InvariantCulture));
        }

        return identity.ToString();
    }

    public static byte[] HashOf(string identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return SHA256.HashData(Encoding.UTF8.GetBytes(identity));
    }
}
