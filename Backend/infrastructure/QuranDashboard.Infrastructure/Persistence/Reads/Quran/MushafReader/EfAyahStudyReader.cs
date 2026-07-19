using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfAyahStudyReader(QuranDashboardDbContext db, ILogger<EfAyahStudyReader> logger) : IAyahStudyReader
{
    private static readonly JsonSerializerOptions CoveredKeysJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AyahStudyResponse?> GetAyahStudyAsync(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey,
        CancellationToken ct)
    {
        var ayah = await db.QuranAyahs
            .AsNoTracking()
            .Include(a => a.Surah)
            .FirstOrDefaultAsync(a => a.VerseKey == verseKey, ct);

        if (ayah is null)
        {
            return null;
        }

        var sajda = await db.QuranSajdas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AyahId == ayah.Id, ct);

        var ayahCore = MapAyahCore(ayah, sajda);

        var tafsir = tafsirSourceKey is not null
            ? await LoadTafsirAsync(ayah.Id, verseKey, tafsirSourceKey, ct)
            : ResolvedSource<TafsirEntryDto>.NoSource;
        var translation = translationSourceKey is not null
            ? await LoadTranslationAsync(ayah.Id, translationSourceKey, ct)
            : ResolvedSource<TranslationEntryDto>.NoSource;
        var fullI3rab = fullI3rabSourceKey is not null
            ? await LoadFullI3rabAsync(ayah.Id, verseKey, fullI3rabSourceKey, ct)
            : ResolvedSource<FullI3rabEntryDto>.NoSource;
        var similaritySummary = await LoadSimilaritySummaryAsync(ayah.Id, ct);

        return new AyahStudyResponse(
            ayahCore,
            new SelectedSourcesDto(
                tafsir.ResolvedKey,
                translation.ResolvedKey,
                fullI3rab.ResolvedKey),
            tafsir.Entry,
            translation.Entry,
            fullI3rab.Entry,
            similaritySummary);
    }

    /// <summary>
    /// Similar-ayah count, mutashabihat group count, and mutashabihat occurrence count are three
    /// independent aggregates over unrelated tables. They are combined into one projection (one
    /// round trip) instead of two-or-three sequential count commands; occurrence count naturally
    /// evaluates to 0 when there are no groups, so the prior "skip when group count is 0" branch
    /// is preserved in effect without needing a second command.
    /// </summary>
    private async Task<SimilaritySummaryDto> LoadSimilaritySummaryAsync(int ayahId, CancellationToken ct)
    {
        var outgoing = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.SourceAyahId == ayahId)
            .Select(link => link.TargetAyahId);

        var incoming = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.TargetAyahId == ayahId)
            .Select(link => link.SourceAyahId);

        var similarAyahIds = outgoing.Union(incoming).Distinct();

        var groupIds = db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.AyahId == ayahId)
            .Select(occurrence => occurrence.GroupId)
            .Distinct();

        var summary = await db.QuranAyahs
            .AsNoTracking()
            .Where(a => a.Id == ayahId)
            .Select(a => new
            {
                SimilarAyahCount = similarAyahIds.Count(),
                GroupCount = groupIds.Count(),
                OccurrenceCount = db.MutashabihatOccurrences
                    .AsNoTracking()
                    .Count(occurrence => groupIds.Contains(occurrence.GroupId)),
            })
            .FirstOrDefaultAsync(ct);

        return new SimilaritySummaryDto(
            summary?.SimilarAyahCount ?? 0,
            summary?.GroupCount ?? 0,
            summary?.OccurrenceCount ?? 0);
    }

    private static AyahCoreDto MapAyahCore(Ayah ayah, Sajda? sajda) => new(
        ayah.VerseKey,
        ayah.SurahNumber,
        ayah.Surah.NameArabic,
        ayah.AyahNumber,
        ayah.TextUthmani,
        ayah.WordsCountReal,
        ayah.PageFrom,
        ayah.PageTo,
        ayah.JuzNumber ?? 0,
        ayah.HizbNumber ?? 0,
        ayah.RubNumber ?? 0,
        sajda is null
            ? null
            : new SajdaDto(
                sajda.SajdahNumber,
                sajda.VerseKey,
                MapSajdahType(sajda.SajdahType)));

    /// <summary>
    /// Tafsir is source -> mapping -> text. Instead of three sequential lookups, one LEFT-JOIN
    /// projection resolves the source, its ayah mapping, and the shared text row together. A
    /// missing mapping or missing text row both surface as null joined columns, which collapses
    /// to the same "resolved key, null block" outcome the sequential version produced.
    /// </summary>
    private async Task<ResolvedSource<TafsirEntryDto>> LoadTafsirAsync(int ayahId, string verseKey, string sourceKey, CancellationToken ct)
    {
        var projection = await (
            from source in db.TafsirSources.AsNoTracking()
            where source.SourceKey == sourceKey
            join ae in db.TafsirAyahEntries.AsNoTracking()
                on new { SourceId = source.Id, AyahId = ayahId } equals new { ae.SourceId, ae.AyahId }
                into ayahEntryGroup
            from ayahEntry in ayahEntryGroup.DefaultIfEmpty()
            join te in db.TafsirEntries.AsNoTracking()
                on ayahEntry.TafsirEntryId equals te.Id
                into entryGroup
            from entry in entryGroup.DefaultIfEmpty()
            select new TafsirProjection(
                source.SourceKey,
                source.DisplayNameAr,
                source.ShortNameAr,
                source.LanguageCode,
                source.Direction,
                source.TafsirKind,
                ayahEntry != null ? ayahEntry.SourceValueKind : null,
                ayahEntry != null ? ayahEntry.SourceLeaderVerseKey : null,
                ayahEntry != null ? (bool?)ayahEntry.IsGroupLeader : null,
                entry != null ? (short?)entry.CoveredAyahCount : null,
                entry != null ? entry.CoveredAyahKeys : null,
                entry != null ? entry.TafsirText : null))
            .FirstOrDefaultAsync(ct);

        if (projection is null)
        {
            return ResolvedSource<TafsirEntryDto>.NoSource;
        }

        if (projection.TafsirText is null)
        {
            return new ResolvedSource<TafsirEntryDto>(projection.SourceKey, null);
        }

        var dto = new TafsirEntryDto(
            projection.SourceKey,
            projection.DisplayNameAr,
            string.IsNullOrWhiteSpace(projection.ShortNameAr) ? null : projection.ShortNameAr,
            projection.LanguageCode,
            projection.Direction,
            projection.TafsirKind,
            projection.SourceValueKind!,
            projection.SourceLeaderVerseKey!,
            projection.IsGroupLeader!.Value,
            projection.CoveredAyahCount!.Value,
            ParseCoveredAyahKeys(projection.CoveredAyahKeys!, verseKey, sourceKey),
            projection.TafsirText);

        return new ResolvedSource<TafsirEntryDto>(projection.SourceKey, dto);
    }

    /// <summary>
    /// Translation is source -> mapping only. One LEFT-JOIN projection replaces the two
    /// sequential lookups; a missing mapping surfaces as a null joined text column.
    /// </summary>
    private async Task<ResolvedSource<TranslationEntryDto>> LoadTranslationAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var projection = await (
            from source in db.TranslationSources.AsNoTracking()
            where source.SourceKey == sourceKey
            join ae in db.TranslationAyahEntries.AsNoTracking()
                on new { SourceId = source.Id, AyahId = ayahId } equals new { ae.SourceId, ae.AyahId }
                into ayahEntryGroup
            from ayahEntry in ayahEntryGroup.DefaultIfEmpty()
            select new TranslationProjection(
                source.SourceKey,
                source.DisplayNameAr,
                source.DisplayNameEn,
                source.LanguageCode,
                source.Direction,
                source.TranslationType,
                source.ContainsHtmlMarkup,
                ayahEntry != null ? ayahEntry.Text : null))
            .FirstOrDefaultAsync(ct);

        if (projection is null)
        {
            return ResolvedSource<TranslationEntryDto>.NoSource;
        }

        if (projection.Text is null)
        {
            return new ResolvedSource<TranslationEntryDto>(projection.SourceKey, null);
        }

        var dto = new TranslationEntryDto(
            projection.SourceKey,
            string.IsNullOrWhiteSpace(projection.DisplayNameAr) ? null : projection.DisplayNameAr,
            string.IsNullOrWhiteSpace(projection.DisplayNameEn) ? null : projection.DisplayNameEn,
            projection.LanguageCode,
            projection.Direction,
            projection.TranslationType,
            projection.ContainsHtmlMarkup,
            projection.Text);

        return new ResolvedSource<TranslationEntryDto>(projection.SourceKey, dto);
    }

    /// <summary>
    /// Full i3rab is source -> mapping -> text, mirroring tafsir's shape via its own tables.
    /// </summary>
    private async Task<ResolvedSource<FullI3rabEntryDto>> LoadFullI3rabAsync(int ayahId, string verseKey, string sourceKey, CancellationToken ct)
    {
        var projection = await (
            from source in db.FullI3rabSources.AsNoTracking()
            where source.SourceKey == sourceKey
            join ae in db.FullI3rabAyahEntries.AsNoTracking()
                on new { SourceId = source.Id, AyahId = ayahId } equals new { ae.SourceId, ae.AyahId }
                into ayahEntryGroup
            from ayahEntry in ayahEntryGroup.DefaultIfEmpty()
            join fe in db.FullI3rabEntries.AsNoTracking()
                on ayahEntry.EntryId equals fe.Id
                into entryGroup
            from entry in entryGroup.DefaultIfEmpty()
            select new FullI3rabProjection(
                source.SourceKey,
                source.DisplayNameAr,
                source.ShortNameAr,
                source.MarkupFormat,
                ayahEntry != null ? ayahEntry.SourceValueKind : null,
                ayahEntry != null ? ayahEntry.SourceLeaderVerseKey : null,
                ayahEntry != null ? (bool?)ayahEntry.IsGroupLeader : null,
                entry != null ? (short?)entry.CoveredAyahCount : null,
                entry != null ? entry.CoveredAyahKeys : null,
                entry != null ? entry.I3rabHtml : null))
            .FirstOrDefaultAsync(ct);

        if (projection is null)
        {
            return ResolvedSource<FullI3rabEntryDto>.NoSource;
        }

        if (projection.I3rabHtml is null)
        {
            return new ResolvedSource<FullI3rabEntryDto>(projection.SourceKey, null);
        }

        var dto = new FullI3rabEntryDto(
            projection.SourceKey,
            projection.DisplayNameAr,
            string.IsNullOrWhiteSpace(projection.ShortNameAr) ? null : projection.ShortNameAr,
            projection.MarkupFormat,
            projection.SourceValueKind!,
            projection.SourceLeaderVerseKey!,
            projection.IsGroupLeader!.Value,
            projection.CoveredAyahCount!.Value,
            ParseCoveredAyahKeys(projection.CoveredAyahKeys!, verseKey, sourceKey),
            projection.I3rabHtml);

        return new ResolvedSource<FullI3rabEntryDto>(projection.SourceKey, dto);
    }

    private sealed record ResolvedSource<T>(string? ResolvedKey, T? Entry)
    {
        public static ResolvedSource<T> NoSource => new(null, default);
    }

    private sealed record TafsirProjection(
        string SourceKey,
        string DisplayNameAr,
        string ShortNameAr,
        string LanguageCode,
        string Direction,
        string TafsirKind,
        string? SourceValueKind,
        string? SourceLeaderVerseKey,
        bool? IsGroupLeader,
        short? CoveredAyahCount,
        string? CoveredAyahKeys,
        string? TafsirText);

    private sealed record TranslationProjection(
        string SourceKey,
        string DisplayNameAr,
        string DisplayNameEn,
        string LanguageCode,
        string Direction,
        string TranslationType,
        bool ContainsHtmlMarkup,
        string? Text);

    private sealed record FullI3rabProjection(
        string SourceKey,
        string DisplayNameAr,
        string ShortNameAr,
        string MarkupFormat,
        string? SourceValueKind,
        string? SourceLeaderVerseKey,
        bool? IsGroupLeader,
        short? CoveredAyahCount,
        string? CoveredAyahKeys,
        string? I3rabHtml);

    /// <summary>
    /// M15 (quran-safety rule 3): corrupt covered_ayah_keys JSON must not be swallowed silently.
    /// The return contract is unchanged (still empty on failure) — a "coverage unavailable"
    /// marker would be a DTO/contract change and is out of scope here — but a corrupt row now
    /// logs a Warning naming the ayah and source so the underlying data issue stays visible.
    /// </summary>
    private IReadOnlyList<string> ParseCoveredAyahKeys(string json, string verseKey, string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, CoveredKeysJsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Corrupt covered_ayah_keys JSON for ayah {verseKey} source {sourceKey}; treating as empty",
                verseKey,
                sourceKey);
            return [];
        }
    }

    private static string MapSajdahType(SajdahType sajdahType) => sajdahType switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => throw new ArgumentOutOfRangeException(nameof(sajdahType), sajdahType, "Unknown sajdah type."),
    };
}
