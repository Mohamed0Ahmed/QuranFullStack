namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseSearchSpellingNormalizer
{
    internal static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Normalize(NormalizationForm.FormC);
        var builder = new StringBuilder(normalized.Length);

        foreach (var rune in normalized.EnumerateRunes())
        {
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

        return builder.ToString();
    }
}
