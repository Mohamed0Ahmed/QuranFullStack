using System.Collections.Frozen;
using System.Text;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

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

        // Consonants (36)
        entries['\''] = "\u0621"; // ء  HAMZA
        entries['|']  = "\u0622"; // آ  ALEF WITH MADDA ABOVE
        entries['>']  = "\u0623"; // أ  ALEF WITH HAMZA ABOVE
        entries['&']  = "\u0624"; // ؤ  WAW WITH HAMZA ABOVE
        entries['<']  = "\u0625"; // إ  ALEF WITH HAMZA BELOW
        entries['}']  = "\u0626"; // ئ  YEH WITH HAMZA ABOVE
        entries['A']  = "\u0627"; // ا  ALEF
        entries['b']  = "\u0628"; // ب  BEH
        entries['p']  = "\u0629"; // ة  TEH MARBUTA
        entries['t']  = "\u062A"; // ت  TEH
        entries['v']  = "\u062B"; // ث  THEH
        entries['j']  = "\u062C"; // ج  JEEM
        entries['H']  = "\u062D"; // ح  HAH
        entries['x']  = "\u062E"; // خ  KHAH
        entries['d']  = "\u062F"; // د  DAL
        entries['*']  = "\u0630"; // ذ  THAL
        entries['r']  = "\u0631"; // ر  REH
        entries['z']  = "\u0632"; // ز  ZAIN
        entries['s']  = "\u0633"; // س  SEEN
        entries['$']  = "\u0634"; // ش  SHEEN
        entries['S']  = "\u0635"; // ص  SAD
        entries['D']  = "\u0636"; // ض  DAD
        entries['T']  = "\u0637"; // ط  TAH
        entries['Z']  = "\u0638"; // ظ  ZAH
        entries['E']  = "\u0639"; // ع  AIN
        entries['g']  = "\u063A"; // غ  GHAIN
        entries['f']  = "\u0641"; // ف  FEH
        entries['q']  = "\u0642"; // ق  QAF
        entries['k']  = "\u0643"; // ك  KAF
        entries['l']  = "\u0644"; // ل  LAM
        entries['m']  = "\u0645"; // م  MEEM
        entries['n']  = "\u0646"; // ن  NOON
        entries['h']  = "\u0647"; // ه  HEH
        entries['w']  = "\u0648"; // و  WAW
        entries['Y']  = "\u0649"; // ى  ALEF MAKSURA
        entries['y']  = "\u064A"; // ي  YEH

        // Harakat / Tanwin / Shadda / Sukun (8)
        entries['a']  = "\u064E"; // َ  FATHAH
        entries['u']  = "\u064F"; // ُ  DAMMAH
        entries['i']  = "\u0650"; // ِ  KASRAH
        entries['F']  = "\u064B"; // ً  FATHATAN
        entries['N']  = "\u064C"; // ٌ  DAMMATAN
        entries['K']  = "\u064D"; // ٍ  KASRATAN
        entries['~']  = "\u0651"; // ّ  SHADDA
        entries['o']  = "\u0652"; // ْ  SUKUN

        // Quranic letter specials (5)
        entries['{']  = "\u0671"; // ٱ  ALEF WASLA
        entries['`']  = "\u0670"; // ٰ  SUPERSCRIPT ALEF
        entries['_']  = "\u0640"; // ـ  TATWEEL
        entries['^']  = "\u0653"; // ٓ  MADDAH ABOVE
        entries['#']  = "\u0654"; // ٔ  HAMZA ABOVE

        // Quranic annotation marks (12)
        entries['@']  = "\u06DF"; // ۟  SMALL HIGH ROUNDED ZERO
        entries[',']  = "\u06E5"; // ۥ  SMALL WAW
        entries['.']  = "\u06E6"; // ۦ  SMALL YEH
        entries['[']  = "\u06E2"; // ۢ  SMALL HIGH MEEM
        entries[']']  = "\u06ED"; // ۭ  SMALL LOW MEEM
        entries['"']  = "\u06E0"; // ۠  SMALL HIGH UPRIGHT ZERO
        entries[':']  = "\u06DC"; // ۜ  SMALL HIGH SEEN
        entries[';']  = "\u06E3"; // ۣ  SMALL LOW SEEN
        entries['+']  = "\u06EB"; // ۫  SMALL HIGH STOP
        entries['!']  = "\u06E8"; // ۨ  SMALL HIGH NOON
        entries['%']  = "\u06EC"; // ۬  ROUNDED HIGH STOP
        entries['-']  = "\u06EA"; // ۪  LOW STOP

        return entries.ToFrozenDictionary();
    }
}
