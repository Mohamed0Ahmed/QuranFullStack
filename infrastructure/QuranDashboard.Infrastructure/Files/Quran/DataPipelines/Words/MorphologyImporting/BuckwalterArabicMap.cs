using System.Collections.Frozen;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class BuckwalterArabicMap
{
    private static readonly FrozenDictionary<char, string> Map = CreateMap();

    public string? TryMap(char buckwalterChar) =>
        Map.TryGetValue(buckwalterChar, out var arabic) ? arabic : null;

    public bool IsMapped(char buckwalterChar) =>
        Map.ContainsKey(buckwalterChar);

    public IReadOnlyCollection<char> AllMappedCharacters =>
        Map.Keys;

    public int MapSize => Map.Count;

    public (string Arabic, List<char> Unmapped) Transliterate(string buckwalterForm)
    {
        ArgumentNullException.ThrowIfNull(buckwalterForm);

        var unmapped = new List<char>();
        var sb = new StringBuilder(buckwalterForm.Length * 2);

        foreach (var ch in buckwalterForm)
        {
            if (Map.TryGetValue(ch, out var arabic))
            {
                sb.Append(arabic);
            }
            else
            {
                unmapped.Add(ch);
            }
        }

        return (sb.ToString(), unmapped);
    }

    private static FrozenDictionary<char, string> CreateMap()
    {
        var entries = new Dictionary<char, string>(61);

        entries['\''] = "\u0621";
        entries['|']  = "\u0622";
        entries['>']  = "\u0623";
        entries['&']  = "\u0624";
        entries['<']  = "\u0625";
        entries['}']  = "\u0626";
        entries['A']  = "\u0627";
        entries['b']  = "\u0628";
        entries['p']  = "\u0629";
        entries['t']  = "\u062A";
        entries['v']  = "\u062B";
        entries['j']  = "\u062C";
        entries['H']  = "\u062D";
        entries['x']  = "\u062E";
        entries['d']  = "\u062F";
        entries['*']  = "\u0630";
        entries['r']  = "\u0631";
        entries['z']  = "\u0632";
        entries['s']  = "\u0633";
        entries['$']  = "\u0634";
        entries['S']  = "\u0635";
        entries['D']  = "\u0636";
        entries['T']  = "\u0637";
        entries['Z']  = "\u0638";
        entries['E']  = "\u0639";
        entries['g']  = "\u063A";
        entries['f']  = "\u0641";
        entries['q']  = "\u0642";
        entries['k']  = "\u0643";
        entries['l']  = "\u0644";
        entries['m']  = "\u0645";
        entries['n']  = "\u0646";
        entries['h']  = "\u0647";
        entries['w']  = "\u0648";
        entries['Y']  = "\u0649";
        entries['y']  = "\u064A";

        entries['a']  = "\u064E";
        entries['u']  = "\u064F";
        entries['i']  = "\u0650";
        entries['F']  = "\u064B";
        entries['N']  = "\u064C";
        entries['K']  = "\u064D";
        entries['~']  = "\u0651";
        entries['o']  = "\u0652";

        entries['{']  = "\u0671";
        entries['`']  = "\u0670";
        entries['_']  = "\u0640";
        entries['^']  = "\u0653";
        entries['#']  = "\u0654";

        entries['@']  = "\u06DF";
        entries[',']  = "\u06E5";
        entries['.']  = "\u06E6";
        entries['[']  = "\u06E2";
        entries[']']  = "\u06ED";
        entries['"']  = "\u06E0";
        entries[':']  = "\u06DC";
        entries[';']  = "\u06E3";
        entries['+']  = "\u06EB";
        entries['!']  = "\u06E8";
        entries['%']  = "\u06EC";
        entries['-']  = "\u06EA";

        return entries.ToFrozenDictionary();
    }
}
