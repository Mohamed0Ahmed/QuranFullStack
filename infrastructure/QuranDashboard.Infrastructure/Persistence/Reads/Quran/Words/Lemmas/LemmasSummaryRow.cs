namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

/// <summary>
/// Flat whole-summary row for one lemma (Feature 016). Identity, owned-root
/// (<c>quran_lemmas.root_id</c>), counts, and the ordered type distribution are
/// all loaded in a single bounded aggregation; <see cref="LemmaTypeDistributionRow"/>
/// entries are ordered count descending then earliest Mushaf occurrence ascending,
/// so the dominant type is the first entry.
/// </summary>
internal sealed record LemmaSummaryRow(
    int Id,
    string LemmaText,
    string? LemmaBuckwalter,
    string NormalizedLemmaText,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount,
    string FirstVerseKey,
    int FirstWordOrderInMushaf,
    IReadOnlyList<LemmaTypeDistributionRow> TypeDistribution);

/// <summary>
/// One POS entry within a lemma's type distribution. Ordered count descending
/// then earliest Mushaf occurrence ascending when materialized.
/// </summary>
internal sealed record LemmaTypeDistributionRow(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber);
