using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Common.Paging;
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

        // Catalogue-driven noun children: every noun-category POS code ordered by SortOrder,
        // each carrying its distinct word-context row count (0 when no rows exist).
        var nounCatalogue = await _dbContext.PosTags.AsNoTracking()
            .Where(pos => pos.Category == NounType)
            .OrderBy(pos => pos.SortOrder)
            .Select(pos => new PosCatalogueRow(pos.Code, pos.ArabicLabel))
            .ToListAsync(cancellationToken);

        var nounChildren = nounCatalogue
            .Select(pos => ChildNode(pos.Code, pos.ArabicLabel, nounChildCounts.GetValueOrDefault(pos.Code)))
            .Where(child => child.Count > 0)
            .ToList();

        // Verb tense children are a fixed v1 set; counts come from the grouped base rows.
        var verbChildren = VerbTenseChildren
            .Select(tense => ChildNode(tense.ChildCode, tense.Label, verbChildCounts.GetValueOrDefault(tense.ChildCode)))
            .Where(child => child.Count > 0)
            .ToList();

        // Particle children are catalogue-driven too; INL stays split into its own main type.
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
            MainNode(NounType, "اسم", nounChildren.Count, "case", nounChildren),
            MainNode(VerbType, "فعل", verbChildren.Count, "tense+voice", verbChildren),
            MainNode(ParticleType, "حرف وأداة", particleChildren.Count, "none", particleChildren),
            MainNode(InlType, "حروف مقطّعة", 1, "none", []),
        ]);
    }

    public async Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var type = NormalizeType(filter.Type);
        var childCode = NormalizeChildCode(filter.ChildCode);
        var context = new WordTypeReadContext(type, childCode, filter.Case, filter.Tense, filter.Voice);
        var totalCount = await CountRowsAsync(context, cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeRowDto>(page, pageSize, totalCount, []);
        }

        var parameters = BuildRowsParameters(context, skip.Value, pageSize);

        var rows = await _dbContext.Database.SqlQueryRaw<WordTypeRowSqlResult>(
            RowsSql(context, sort),
            parameters)
            .ToListAsync(cancellationToken);

        return new PagedResult<WordTypeRowDto>(
            page,
            pageSize,
            totalCount,
            rows.Select(row => row.ToDto()).ToList());
    }

    public async Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
        WordTypeFilter filter,
        WordTypeTableView tableView,
        WordTypeSort sort,
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
        var context = new WordTypeReadContext(type, childCode, filter.Case, filter.Tense, filter.Voice);
        var totalCount = await CountGroupedRowsAsync(context, tableView, cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeTableRowDto>(page, pageSize, totalCount, []);
        }

        var parameters = BuildGroupedRowsParameters(context, sort, skip.Value, pageSize);
        var rows = await _dbContext.Database.SqlQueryRaw<GroupedRowSqlResult>(
            GroupedRowsSql(context, tableView, sort),
            parameters)
            .ToListAsync(cancellationToken);

        return new PagedResult<WordTypeTableRowDto>(
            page,
            pageSize,
            totalCount,
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
                from morphology in MatchedMorphologyQuery(identity)
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

        if (!await MatchedMorphologyQuery(identity).AnyAsync(cancellationToken))
        {
            return null;
        }

        var matchedAyahIds = MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var pageAyahs = await (
                from ayah in _dbContext.QuranAyahs.AsNoTracking()
                where matchedAyahIds.Contains(ayah.Id)
                orderby ayah.SurahNumber, ayah.AyahNumber
                select new AyahMetaRow(
                    ayah.Id,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber))
            .Skip(skip.Value)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(ayah => ayah.AyahId).ToList();

        var matchedRows = await MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word)
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

        var wordsByAyah = await _dbContext.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new AyahWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani,
                word.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                var matched = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []);
                var matchedPositions = matched.Select(row => row.WordNumber).Distinct().OrderBy(number => number).ToList();

                return new WordTypeAyahMatchDto(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedPositions,
                    matched.Select(row => row.QuranWordId).ToList(),
                    words.Select(word => new AyahWordForHighlightDto(
                        word.QuranWordId,
                        word.TextUthmani,
                        word.IsAyahMarker)).ToList());
            })
            .ToList();

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

        if (!await MatchedMorphologyQuery(identity).AnyAsync(cancellationToken))
        {
            return null;
        }

        var matchedWords = await MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word.SurahNumber)
            .ToListAsync(cancellationToken);

        var surahGroups = matchedWords
            .GroupBy(surahNumber => surahNumber)
            .Select(group => new SurahOccurrenceRow(group.Key, group.Count()))
            .OrderBy(row => row.SurahNumber)
            .ToList();

        var mentionedNumbers = surahGroups.Select(row => row.SurahNumber).ToList();
        var surahNames = await _dbContext.QuranSurahs
            .AsNoTracking()
            .ToDictionaryAsync(surah => surah.SurahNumber, surah => surah.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(row => new WordTypeSurahOccurrenceDto(
                (int)row.SurahNumber,
                surahNames[row.SurahNumber],
                row.OccurrencesCount))
            .ToList();

        var missingSurahs = await _dbContext.QuranSurahs
            .AsNoTracking()
            .Where(surah => !mentionedNumbers.Contains(surah.SurahNumber))
            .OrderBy(surah => surah.SurahNumber)
            .Select(surah => new WordTypeMissingSurahDto((int)surah.SurahNumber, surah.NameArabic))
            .ToListAsync(cancellationToken);

        return new WordTypeSurahsResponse(surahs, missingSurahs);
    }

    private IQueryable<Domain.Quran.Words.Morphology.WordMorphology> MatchedMorphologyQuery(WordTypeRowIdentity identity) =>
        from morphology in _dbContext.WordMorphologies.AsNoTracking()
        join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
        where !word.IsAyahMarker
            && word.UniqueTashkeelWordId == identity.TashkeelWordId
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

    private sealed record AyahMetaRow(int AyahId, string VerseKey, int SurahNumber, int AyahNumber);

    private sealed record MatchedWordRow(int AyahId, int QuranWordId, int WordNumber);

    private sealed record AyahWordRow(int AyahId, int QuranWordId, int WordNumber, short PageNumber, string TextUthmani, bool IsAyahMarker);

    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadableWord = words.FirstOrDefault(word => !word.IsAyahMarker);
        if (firstReadableWord is not null)
        {
            return firstReadableWord.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesCount);

    private sealed record PosCatalogueRow(string Code, string ArabicLabel);

    // Verb tense child nodes are a fixed v1 set (noun children are catalogue-driven).
    private static readonly (string ChildCode, string Label)[] VerbTenseChildren =
    [
        ("past", "ماض"),
        ("present", "مضارع"),
        ("imperative", "أمر"),
    ];
}
