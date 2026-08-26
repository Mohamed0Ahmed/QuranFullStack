using System.Globalization;
using System.Text;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

internal static class PhraseSearchInputNormalizer
{
    internal static string NormalizeSegment(string value, PhraseTextMode mode)
    {
        var normalized = value.Normalize(NormalizationForm.FormC);
        var builder = new StringBuilder(normalized.Length);

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (IsIgnoredIdentityMark(rune.Value)
                || mode == PhraseTextMode.Simple && IsCombiningMark(rune))
            {
                continue;
            }

            switch (rune.Value)
            {
                case 0x0622:
                case 0x0623:
                case 0x0625:
                case 0x0671:
                    builder.Append('\u0627');
                    break;
                case 0x0624:
                    builder.Append('\u0648');
                    break;
                case 0x0626:
                    builder.Append('\u064A');
                    break;
                case 0x0621:
                    break;
                default:
                    builder.Append(rune.ToString());
                    break;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsCombiningMark(Rune rune) => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.NonSpacingMark
        or UnicodeCategory.SpacingCombiningMark
        or UnicodeCategory.EnclosingMark;

    private static bool IsIgnoredIdentityMark(int value) => value is
        0x0640
        or 0x0653
        or >= 0x06D6 and <= 0x06DC
        or 0x06DE
        or 0x06E9
        or 0x200F;
}
