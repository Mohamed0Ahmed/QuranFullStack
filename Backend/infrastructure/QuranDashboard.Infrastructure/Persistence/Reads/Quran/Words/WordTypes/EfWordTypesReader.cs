using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader(QuranDashboardDbContext dbContext) : IWordTypesReader
{
    private const string NounType = "noun";
    private const string VerbType = "verb";
    private const string ParticleType = "particle";
    private const string InlType = "inl";
    private const string InlPos = "INL";
    private const string UnspecifiedContext = WordTypeRowContext.Unspecified;

    private readonly QuranDashboardDbContext _dbContext = dbContext;

    public async Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        var childCounts = await _dbContext.Database.SqlQueryRaw<TreeChildCountRow>(TreeChildCountsSql())
            .ToListAsync(cancellationToken);

        var nounChildCounts = childCounts
            .Where(row => row.Type == NounType)
            .ToDictionary(row => row.ChildCode, row => row.Count);
        var verbChildCounts = childCounts
            .Where(row => row.Type == VerbType)
            .ToDictionary(row => row.ChildCode, row => row.Count);
        var particleChildCounts = childCounts
            .Where(row => row.Type == ParticleType)
            .ToDictionary(row => row.ChildCode, row => row.Count);
        var mainCounts = childCounts
            .GroupBy(row => row.Type)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Count));

        var nounCatalogue = await _dbContext.PosTags.AsNoTracking()
            .Where(pos => pos.Category == NounType)
            .OrderBy(pos => pos.SortOrder)
            .Select(pos => new PosCatalogueRow(pos.Code, pos.ArabicLabel))
            .ToListAsync(cancellationToken);

        var nounChildren = nounCatalogue
            .Select(pos => ChildNode(pos.Code, pos.ArabicLabel, nounChildCounts.GetValueOrDefault(pos.Code)))
            .Where(child => child.Count > 0)
            .ToList();

        var verbChildren = VerbTenseChildren
            .Select(tense => ChildNode(tense.ChildCode, tense.Label, verbChildCounts.GetValueOrDefault(tense.ChildCode)))
            .Where(child => child.Count > 0)
            .ToList();

        var particleCatalogue = await _dbContext.PosTags.AsNoTracking()
            .Where(pos => pos.Category == ParticleType && pos.Code != InlPos)
            .OrderBy(pos => pos.SortOrder)
            .Select(pos => new PosCatalogueRow(pos.Code, pos.ArabicLabel))
            .ToListAsync(cancellationToken);

        var particleChildren = particleCatalogue
            .Select(pos => ChildNode(pos.Code, pos.ArabicLabel, particleChildCounts.GetValueOrDefault(pos.Code)))
            .Where(child => child.Count > 0)
            .ToList();

        return new WordTypeTreeDto([
            MainNode(NounType, "اسم", mainCounts.GetValueOrDefault(NounType), "case", nounChildren),
            MainNode(VerbType, "فعل", mainCounts.GetValueOrDefault(VerbType), "tense+voice", verbChildren),
            MainNode(ParticleType, "حرف وأداة", mainCounts.GetValueOrDefault(ParticleType), "none", particleChildren),
            MainNode(InlType, "حروف مقطّعة", mainCounts.GetValueOrDefault(InlType), "none", []),
        ]);
    }

    public async Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSortSpec sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var type = NormalizeType(filter.Type);
        var childCode = NormalizeChildCode(filter.ChildCode);
        var context = new WordTypeReadContext(type, childCode, filter.Case, filter.Tense, filter.Voice, ArabicSearchQueryNormalizer.Normalize(filter.Search), filter.HasRoot, filter.HasStem, filter.HasLemma);

        // B1: page + total come from ONE scoped command via COUNT(*) OVER(). The count-only fallback runs
        // only for an empty page (out-of-range or empty scope), where the window count has no row to carry it.
        var offset = ReadPaging.CalculatePageOffset(page, pageSize);
        if (offset is null)
        {
            return new PagedResult<WordTypeRowDto>(page, pageSize, await CountRowsAsync(context, cancellationToken), []);
        }

        var parameters = BuildRowsParameters(context, offset.Value, pageSize);
        var rows = await _dbContext.Database.SqlQueryRaw<WordTypeRowSqlResult>(
            RowsSql(context, sort),
            parameters)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PagedResult<WordTypeRowDto>(page, pageSize, await CountRowsAsync(context, cancellationToken), []);
        }

        return new PagedResult<WordTypeRowDto>(
            page,
            pageSize,
            rows[0].TotalCount,
            rows.Select(row => row.ToDto()).ToList());
    }

    public async Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
        WordTypeFilter filter,
        WordTypeTableView tableView,
        WordTypeSortSpec sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (tableView == WordTypeTableView.Words)
        {
            var wordPage = await GetRowsAsync(filter, sort, page, pageSize, cancellationToken);
            return new PagedResult<WordTypeTableRowDto>(
                wordPage.Page,
                wordPage.PageSize,
                wordPage.TotalCount,
                wordPage.Items.Select(row => ToWordTableRowDto(row, filter)).ToList());
        }

        var type = NormalizeType(filter.Type);
        var childCode = NormalizeChildCode(filter.ChildCode);
        var context = new WordTypeReadContext(type, childCode, filter.Case, filter.Tense, filter.Voice, ArabicSearchQueryNormalizer.Normalize(filter.Search), filter.HasRoot, filter.HasStem, filter.HasLemma);

        // B1: same single-command window-count path as the words view, with the count-only fallback reserved
        // for the exceptional empty page.
        var offset = ReadPaging.CalculatePageOffset(page, pageSize);
        if (offset is null)
        {
            return new PagedResult<WordTypeTableRowDto>(page, pageSize, await CountGroupedRowsAsync(context, tableView, cancellationToken), []);
        }

        var parameters = BuildGroupedRowsParameters(context, sort, offset.Value, pageSize);
        var rows = await _dbContext.Database.SqlQueryRaw<GroupedRowSqlResult>(
            GroupedRowsSql(context, tableView, sort),
            parameters)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PagedResult<WordTypeTableRowDto>(page, pageSize, await CountGroupedRowsAsync(context, tableView, cancellationToken), []);
        }

        return new PagedResult<WordTypeTableRowDto>(
            page,
            pageSize,
            rows[0].TotalCount,
            rows.Select(row => row.ToDto(tableView)).ToList());
    }

    private async Task<int> CountGroupedRowsAsync(WordTypeReadContext context, WordTypeTableView tableView, CancellationToken cancellationToken)
    {
        var parameters = BuildCountParameters(context);
        var result = await _dbContext.Database.SqlQueryRaw<CountRow>(GroupedRowsCountSql(context, tableView), parameters)
            .SingleAsync(cancellationToken);

        return result.Count;
    }

    // The word variant's Case/Tense/Voice complete the composite identity using the active filter that
    // scoped this page — the same values the detail endpoints (summary/ayahs/surahs) already accept.
    private static WordTableRowDto ToWordTableRowDto(WordTypeRowDto row, WordTypeFilter filter) => new(
        row.TashkeelWordId,
        row.ContextCode,
        filter.Case,
        filter.Tense,
        filter.Voice,
        row.DisplayText,
        row.TypeCode,
        row.TypeLabel,
        row.BroadLabel,
        row.CaseOrFeature,
        row.RootText,
        row.LemmaText,
        row.StemText,
        row.OccurrencesCount,
        row.AyahsCount,
        row.SurahsCount);

    public async Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        var rows = await (
                from morphology in MatchedMorphologyQuery(_dbContext, identity)
                join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
                join uniqueWord in _dbContext.QuranWordsUniqueTashkeel.AsNoTracking()
                    on word.UniqueTashkeelWordId equals uniqueWord.Id
                join pos in _dbContext.PosTags.AsNoTracking() on morphology.HeadPos equals pos.Code
                join root in _dbContext.QuranRoots.AsNoTracking() on morphology.RootId equals root.Id into roots
                from root in roots.DefaultIfEmpty()
                join lemma in _dbContext.QuranLemmas.AsNoTracking() on morphology.LemmaId equals lemma.Id into lemmas
                from lemma in lemmas.DefaultIfEmpty()
                join stem in _dbContext.QuranStems.AsNoTracking() on morphology.StemId equals stem.Id into stems
                from stem in stems.DefaultIfEmpty()
                select new SummarySourceRow(
                    word.Id,
                    word.AyahId,
                    word.SurahNumber,
                    uniqueWord.TextUthmani,
                    morphology.HeadPos,
                    pos.ArabicLabel,
                    pos.Category,
                    morphology.VerbTense,
                    morphology.CaseFeature,
                    root != null ? root.RootText : null,
                    lemma != null ? lemma.LemmaText : null,
                    stem != null ? stem.StemText : null,
                    morphology.RootId,
                    morphology.LemmaId,
                    morphology.StemId))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows.OrderBy(row => row.QuranWordId).First();
        var typeLabel = ResolveTypeLabel(first, identity.ContextCode);
        var broadLabel = ResolveBroadLabelFromCategory(first.PosCategory, first.HeadPos);
        var caseOrFeature = WordTypeIdentityMatcher.IsVerbContextCode(identity.ContextCode)
            ? identity.ContextCode
            : first.CaseFeature;

        return new WordTypeSummaryDto(
            identity.TashkeelWordId,
            identity.ContextCode,
            first.DisplayText,
            new WordTypeLabelDto(typeLabel),
            new WordTypeLabelDto(broadLabel),
            caseOrFeature,
            SelectWinner(rows, row => row.RootText, row => row.RootId),
            SelectWinner(rows, row => row.LemmaText, row => row.LemmaId),
            SelectWinner(rows, row => row.StemText, row => row.StemId),
            rows.Count,
            rows.Select(row => row.AyahId).Distinct().Count(),
            rows.Select(row => row.SurahNumber).Distinct().Count());
    }

    public async Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        // One matched-word projection, reused for the distinct-ayah set and the page's matched rows.
        var matchedWords = MatchedWordsQuery(identity);
        var matchedAyahIds = matchedWords.Select(word => word.AyahId).Distinct();

        // B3: the distinct-ayah count doubles as the existence check (zero → identity absent → null),
        // so the preliminary AnyAsync probe is gone.
        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return null;
        }

        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var pageAyahs = await (
                from ayah in _dbContext.QuranAyahs.AsNoTracking()
                join surah in _dbContext.QuranSurahs.AsNoTracking()
                    on ayah.SurahNumber equals surah.SurahNumber
                where matchedAyahIds.Contains(ayah.Id)
                orderby ayah.SurahNumber, ayah.AyahNumber
                select new AyahMetaRow(
                    ayah.Id,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    surah.NameArabic))
            .Skip(skip.Value)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(ayah => ayah.AyahId).ToList();

        var matchedRows = await matchedWords
            .Where(word => ayahIds.Contains(word.AyahId))
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .ThenBy(word => word.Id)
            .Select(word => new MatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = await AyahWordHydration.ProjectAyahMatchesAsync(
            _dbContext,
            pageAyahs,
            ayah => ayah.AyahId,
            (ayah, words, pageNumber) =>
            {
                var matched = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []);
                var matchedPositions = matched.Select(row => row.WordNumber).Distinct().OrderBy(number => number).ToList();

                return new WordTypeAyahMatchDto(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.SurahNameArabic,
                    ayah.AyahNumber,
                    pageNumber,
                    matchedPositions,
                    matched.Select(row => row.QuranWordId).ToList(),
                    words.Select(word => new AyahWordForHighlightDto(
                        word.QuranWordId,
                        word.TextUthmani,
                        word.IsAyahMarker)).ToList());
            },
            cancellationToken);

        return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        // B3: mirror the grouped surahs read. One server-side aggregate groups the scoped occurrences by
        // surah; zero rows means the identity is absent from the scope (not found) and short-circuits before
        // the catalogue read — the aggregate doubles as the existence check, replacing the AnyAsync probe.
        var occurrences = await MatchedWordsQuery(identity)
            .GroupBy(word => word.SurahNumber)
            .Select(group => new SurahOccurrenceRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        if (occurrences.Count == 0)
        {
            return null;
        }

        var occurrencesByNumber = occurrences.ToDictionary(row => row.SurahNumber, row => row.OccurrencesCount);

        // One bounded catalogue read supplies every surah number/name once; mentioned and missing lists are
        // derived in memory in numeric order — never a second catalogue query per occurrence.
        var catalogue = await _dbContext.QuranSurahs
            .AsNoTracking()
            .OrderBy(surah => surah.SurahNumber)
            .Select(surah => new SurahCatalogueRow((int)surah.SurahNumber, surah.NameArabic))
            .ToListAsync(cancellationToken);

        var surahs = new List<WordTypeSurahOccurrenceDto>();
        var missingSurahs = new List<WordTypeMissingSurahDto>();
        foreach (var surah in catalogue)
        {
            if (occurrencesByNumber.TryGetValue((short)surah.SurahNumber, out var occurrencesCount))
            {
                surahs.Add(new WordTypeSurahOccurrenceDto(surah.SurahNumber, surah.NameArabic, occurrencesCount));
            }
            else
            {
                missingSurahs.Add(new WordTypeMissingSurahDto(surah.SurahNumber, surah.NameArabic));
            }
        }

        return new WordTypeSurahsResponse(surahs, missingSurahs);
    }

    // Filters through the QuranWord navigation (not an explicit second join) so a matched-word projection
    // (MatchedWordsQuery) reuses this single quran_words join instead of re-joining on the same key.
    internal static IQueryable<Domain.Quran.Words.Morphology.WordMorphology> MatchedMorphologyQuery(
        QuranDashboardDbContext dbContext,
        WordTypeRowIdentity identity) =>
        from morphology in dbContext.WordMorphologies.AsNoTracking()
        where !morphology.QuranWord.IsAyahMarker
            && morphology.QuranWord.UniqueTashkeelWordId == identity.TashkeelWordId
            && (
                (identity.ContextCode == "past"
                    || identity.ContextCode == "present"
                    || identity.ContextCode == "imperative"
                    || identity.ContextCode == UnspecifiedContext)
                    ? morphology.IsVerb
                        && (morphology.VerbTense ?? UnspecifiedContext) == identity.ContextCode
                    : morphology.HeadPos == identity.ContextCode)
            && (identity.Case == null
                || identity.Case == "all"
                || (identity.Case == "null" ? morphology.CaseFeature == null : morphology.CaseFeature == identity.Case))
            && (identity.Tense == null
                || identity.Tense == "all"
                || morphology.VerbTense == identity.Tense)
            && (identity.Voice == null
                || identity.Voice == "all"
                || morphology.VerbVoice == identity.Voice)
        select morphology;

    // Matched head words for this row identity, projected once from the shared morphology scope so callers
    // reuse a single quran_words join (via the QuranWord navigation) instead of re-joining it per read.
    // Returns the word entities themselves so each consumer composes its own aggregate/projection.
    private IQueryable<Domain.Quran.Words.QuranWord> MatchedWordsQuery(WordTypeRowIdentity identity) =>
        MatchedMorphologyQuery(_dbContext, identity).Select(morphology => morphology.QuranWord);

    private async Task<int> CountRowsAsync(WordTypeReadContext context, CancellationToken cancellationToken)
    {
        var parameters = BuildCountParameters(context);
        var result = await _dbContext.Database.SqlQueryRaw<CountRow>(RowsCountSql(context), parameters)
            .SingleAsync(cancellationToken);

        return result.Count;
    }

    private static WordTypeTreeNodeDto MainNode(
        string code,
        string label,
        int count,
        string secondaryKind,
        IReadOnlyList<WordTypeChildNodeDto> children) =>
        new(
            code,
            new WordTypeLabelDto(label),
            count,
            new WordTypeSecondaryFilterDto(secondaryKind, SecondaryOptions(secondaryKind), VoiceOptions(secondaryKind)),
            children);

    private static WordTypeChildNodeDto ChildNode(string code, string label, int count) =>
        new(code, code, new WordTypeLabelDto(label), count);

    private static IReadOnlyList<WordTypeFilterOptionDto> SecondaryOptions(string secondaryKind) => secondaryKind switch
    {
        "case" => [
            Option("nominative", "مرفوع"),
            Option("accusative", "منصوب"),
            Option("genitive", "مجرور"),
            Option("null", "غير محدد"),
        ],
        "tense+voice" => [
            Option("past", "ماض"),
            Option("present", "مضارع"),
            Option("imperative", "أمر"),
        ],
        _ => [],
    };

    private static IReadOnlyList<WordTypeFilterOptionDto> VoiceOptions(string secondaryKind) =>
        secondaryKind == "tense+voice"
            ? [Option("active", "معلوم"), Option("passive", "مجهول")]
            : [];

    private static WordTypeFilterOptionDto Option(string code, string label) =>
        new(code, new WordTypeLabelDto(label));

    private static string? SelectWinner<TId>(
        IReadOnlyList<SummarySourceRow> rows,
        Func<SummarySourceRow, string?> textSelector,
        Func<SummarySourceRow, TId?> idSelector)
        where TId : struct
    {
        var candidates = rows
            .Where(row => idSelector(row) is not null && textSelector(row) is not null)
            .GroupBy(row => idSelector(row)!.Value)
            .Select(group => new
            {
                Text = group.Min(row => textSelector(row))!,
                Count = group.Count(),
                FirstWordOrder = group.Min(row => row.QuranWordId),
                Id = group.Key,
            })
            .OrderByDescending(candidate => candidate.Count)
            .ThenBy(candidate => candidate.FirstWordOrder)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();

        return candidates?.Text;
    }

    private static string ResolveTypeLabel(SummarySourceRow row, string contextCode) =>
        WordTypeIdentityMatcher.IsVerbContextCode(contextCode)
            ? contextCode switch
            {
                "past" => "ماض",
                "present" => "مضارع",
                "imperative" => "أمر",
                _ => row.PosLabel,
            }
            : row.PosLabel;

    private static string ResolveBroadLabelFromCategory(string category, string headPos) => category switch
    {
        VerbType => "فعل",
        ParticleType when headPos == InlPos => "حروف مقطّعة",
        ParticleType => "حرف وأداة",
        _ => "اسم",
    };

    private static string NormalizeType(string? type) => string.IsNullOrWhiteSpace(type) ? NounType : type.Trim().ToLowerInvariant();

    // Noun child codes are POS codes (uppercase); verb child codes are tense literals (lowercase).
    // Preserve the raw value rather than lower-casing so noun POS codes like "PN" survive.
    private static string? NormalizeChildCode(string? childCode) =>
        string.IsNullOrWhiteSpace(childCode) ? null : childCode.Trim();

    private sealed record SummarySourceRow(
        int QuranWordId,
        int AyahId,
        int SurahNumber,
        string DisplayText,
        string HeadPos,
        string PosLabel,
        string PosCategory,
        string? VerbTense,
        string? CaseFeature,
        string? RootText,
        string? LemmaText,
        string? StemText,
        int? RootId,
        int? LemmaId,
        int? StemId);

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        int SurahNumber,
        int AyahNumber,
        string SurahNameArabic);

    private sealed record MatchedWordRow(int AyahId, int QuranWordId, int WordNumber);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesCount);

    private sealed record PosCatalogueRow(string Code, string ArabicLabel);

    private static readonly (string ChildCode, string Label)[] VerbTenseChildren =
    [
        ("past", "ماض"),
        ("present", "مضارع"),
        ("imperative", "أمر"),
    ];
}
