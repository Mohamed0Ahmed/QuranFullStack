using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

/// <summary>
/// EF read implementation for one ayah's study context: core identity plus the
/// three selected source kinds loaded together. HTML is returned unmodified;
/// sanitization happens on the frontend at render time.
/// </summary>
public sealed class EfAyahStudyReader(QuranDashboardDbContext db) : IAyahStudyReader
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

        // Each Load* method resolves the source row itself, so the source kind's
        // existence check and the row fetch share a single round-trip (previously
        // the existence was verified twice: once by Resolve*SourceKeyAsync and
        // again when Load* re-fetched the same row). Load* returns the resolved
        // source key separately from the (nullable) content block: the key is
        // echoed in selectedSources whenever the source exists, independent of
        // whether this ayah has content in it (ayah-study.api.md: "selectedSources
        // echoes the resolved key actually used per kind").
        var tafsir = tafsirSourceKey is not null
            ? await LoadTafsirAsync(ayah.Id, tafsirSourceKey, ct)
            : ResolvedSource<TafsirEntryDto>.NoSource;
        var translation = translationSourceKey is not null
            ? await LoadTranslationAsync(ayah.Id, translationSourceKey, ct)
            : ResolvedSource<TranslationEntryDto>.NoSource;
        var fullI3rab = fullI3rabSourceKey is not null
            ? await LoadFullI3rabAsync(ayah.Id, fullI3rabSourceKey, ct)
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

    private async Task<SimilaritySummaryDto> LoadSimilaritySummaryAsync(int ayahId, CancellationToken ct)
    {
        var similarAyahCount = await CountDistinctSimilarAyahsAsync(ayahId, ct);
        var (groupCount, occurrenceCount) = await CountMutashabihatAsync(ayahId, ct);

        return new SimilaritySummaryDto(similarAyahCount, groupCount, occurrenceCount);
    }

    private async Task<int> CountDistinctSimilarAyahsAsync(int ayahId, CancellationToken ct)
    {
        var outgoing = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.SourceAyahId == ayahId)
            .Select(link => link.TargetAyahId);

        var incoming = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.TargetAyahId == ayahId)
            .Select(link => link.SourceAyahId);

        return await outgoing.Union(incoming).Distinct().CountAsync(ct);
    }

    private async Task<(int GroupCount, int OccurrenceCount)> CountMutashabihatAsync(
        int ayahId,
        CancellationToken ct)
    {
        var groupIds = db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.AyahId == ayahId)
            .Select(occurrence => occurrence.GroupId)
            .Distinct();

        var groupCount = await groupIds.CountAsync(ct);
        if (groupCount == 0)
        {
            return (0, 0);
        }

        var occurrenceCount = await db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => groupIds.Contains(occurrence.GroupId))
            .CountAsync(ct);

        return (groupCount, occurrenceCount);
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

    private async Task<ResolvedSource<TafsirEntryDto>> LoadTafsirAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.TafsirSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return ResolvedSource<TafsirEntryDto>.NoSource;
        }

        var ayahEntry = await db.TafsirAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return new ResolvedSource<TafsirEntryDto>(source.SourceKey, null);
        }

        var entry = await db.TafsirEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ayahEntry.TafsirEntryId, ct);

        if (entry is null)
        {
            return new ResolvedSource<TafsirEntryDto>(source.SourceKey, null);
        }

        var dto = new TafsirEntryDto(
            source.SourceKey,
            source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.ShortNameAr) ? null : source.ShortNameAr,
            source.LanguageCode,
            source.Direction,
            source.TafsirKind,
            ayahEntry.SourceValueKind,
            ayahEntry.SourceLeaderVerseKey,
            ayahEntry.IsGroupLeader,
            entry.CoveredAyahCount,
            ParseCoveredAyahKeys(entry.CoveredAyahKeys),
            entry.TafsirText);

        return new ResolvedSource<TafsirEntryDto>(source.SourceKey, dto);
    }

    private async Task<ResolvedSource<TranslationEntryDto>> LoadTranslationAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.TranslationSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return ResolvedSource<TranslationEntryDto>.NoSource;
        }

        var ayahEntry = await db.TranslationAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return new ResolvedSource<TranslationEntryDto>(source.SourceKey, null);
        }

        var dto = new TranslationEntryDto(
            source.SourceKey,
            string.IsNullOrWhiteSpace(source.DisplayNameAr) ? null : source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.DisplayNameEn) ? null : source.DisplayNameEn,
            source.LanguageCode,
            source.Direction,
            source.TranslationType,
            source.ContainsHtmlMarkup,
            ayahEntry.Text);

        return new ResolvedSource<TranslationEntryDto>(source.SourceKey, dto);
    }

    private async Task<ResolvedSource<FullI3rabEntryDto>> LoadFullI3rabAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.FullI3rabSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return ResolvedSource<FullI3rabEntryDto>.NoSource;
        }

        var ayahEntry = await db.FullI3rabAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return new ResolvedSource<FullI3rabEntryDto>(source.SourceKey, null);
        }

        var entry = await db.FullI3rabEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ayahEntry.EntryId, ct);

        if (entry is null)
        {
            return new ResolvedSource<FullI3rabEntryDto>(source.SourceKey, null);
        }

        var dto = new FullI3rabEntryDto(
            source.SourceKey,
            source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.ShortNameAr) ? null : source.ShortNameAr,
            source.MarkupFormat,
            ayahEntry.SourceValueKind,
            ayahEntry.SourceLeaderVerseKey,
            ayahEntry.IsGroupLeader,
            entry.CoveredAyahCount,
            ParseCoveredAyahKeys(entry.CoveredAyahKeys),
            entry.I3rabHtml);

        return new ResolvedSource<FullI3rabEntryDto>(source.SourceKey, dto);
    }

    /// <summary>
    /// Pairs a resolved source key with the (nullable) content block for that kind.
    /// <see cref="ResolvedKey"/> is non-null when the source exists — independent
    /// of whether this ayah has content — so it can be echoed in selectedSources.
    /// </summary>
    private sealed record ResolvedSource<T>(string? ResolvedKey, T? Entry)
    {
        public static ResolvedSource<T> NoSource => new(null, default);
    }

    private static IReadOnlyList<string> ParseCoveredAyahKeys(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, CoveredKeysJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string MapSajdahType(SajdahType sajdahType) => sajdahType switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => "required",
    };
}
