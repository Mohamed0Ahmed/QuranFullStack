using System.Text;

namespace QuranDashboard.Domain.Abwab.Tree;

public static class ArabicNameNormalizer
{
    private const int Tatweel = 0x0640;
    private const int AlefHamzaAbove = 0x0623;
    private const int AlefHamzaBelow = 0x0625;
    private const int AlefMadda = 0x0622;
    private const int AlefWasla = 0x0671;
    private const int Alef = 0x0627;
    private const int AlefMaqsura = 0x0649;
    private const int Yeh = 0x064A;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var formC = value.Normalize(NormalizationForm.FormC);
        var collapsed = CollapseWhitespace(formC);

        var builder = new StringBuilder(collapsed.Length);
        foreach (var rune in collapsed.EnumerateRunes())
        {
            if (rune.Value == Tatweel || IsFrozenMark(rune.Value))
            {
                continue;
            }

            builder.Append(MapScalar(rune));
        }

        return builder.ToString();
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            pendingSpace = false;
            builder.Append(rune);
        }

        return builder.ToString();
    }

    // Master Plan §5.1 frozen Unicode-16 Arabic-mark set: exact ranges only, never a runtime "all marks" predicate.
    private static bool IsFrozenMark(int scalar) =>
        scalar is (>= 0x0610 and <= 0x061A)
            or (>= 0x064B and <= 0x065F)
            or 0x0670
            or (>= 0x06D6 and <= 0x06DC)
            or (>= 0x06DF and <= 0x06E4)
            or (>= 0x06E7 and <= 0x06E8)
            or (>= 0x06EA and <= 0x06ED)
            or (>= 0x0897 and <= 0x089F)
            or (>= 0x08CA and <= 0x08E1)
            or (>= 0x08E3 and <= 0x08FF)
            or (>= 0x10EFC and <= 0x10EFF);

    private static Rune MapScalar(Rune rune) => rune.Value switch
    {
        AlefHamzaAbove or AlefHamzaBelow or AlefMadda or AlefWasla => new Rune(Alef),
        AlefMaqsura => new Rune(Yeh),
        _ => rune,
    };
}
